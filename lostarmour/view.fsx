#r "nuget: Avalonia, 12.1.0"
#r "nuget: Avalonia.Desktop, 12.1.0"
#r "nuget: Avalonia.Themes.Fluent, 12.1.0"
#r "nuget: Avalonia.FuncUI, 2.0.0"
#r "nuget: Avalonia.FuncUI.Elmish, 2.0.0"
#r "nuget: Avalonia.Controls.DataGrid, 12.1.0"
#r "nuget: AvaloniaCommunity.FuncUI.Bindings.DataGrid, 12.0.0"
#r "nuget: Microsoft.Data.Sqlite, 10.0.10"

open System
open System.Collections.Generic
open System.IO
open Microsoft.Data.Sqlite
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.Threading

// ============================================================================
// МОДЕЛИ ДАННЫХ
// ============================================================================

[<CLIMutable>]
type Casualty = {
    Id: int64
    Fullname: string
    AltFullname: string option
    Rank: string option
    Conscription: string option
    DaysFromConscription: int option
    Age: int option
    Sex: string option
    DateOfBirth: DateTime option
    DateOfDeath: DateTime option
    DateOfConscription: DateTime option
    DateOfFuneral: DateTime option
    IsDiedFromWounds: bool
    IsFromDecrees: bool
    DeathAt: DateTime option
    RegionOfLive: string option
    RegionOfDeath: string option
    RegionOfFuneral: string option
    Sources: string list
}

[<CLIMutable>]
type SearchFilters = {
    Fullname: string
    Rank: string option
    Region: string option
    AgeFrom: int option
    AgeTo: int option
    DateOfBirthFrom: DateTimeOffset option  
    DateOfBirthTo: DateTimeOffset option    
    DateOfDeathFrom: DateTimeOffset option  
    DateOfDeathTo: DateTimeOffset option    
    Sex: string option
    IsDiedFromWounds: bool option
    IsFromDecrees: bool option
    Limit: int
    Offset: int
}

type SearchResult = {
    Items: Casualty list
    TotalCount: int
    Page: int
    TotalPages: int
}

type Model = {
    Filters: SearchFilters
    Results: Casualty list
    TotalCount: int
    CurrentPage: int
    PageSize: int
    IsLoading: bool
    StatusMessage: string
    SelectedCasualty: Casualty option
    AvailableRanks: string list
    AvailableRegions: string list
    AvailableSex: string list
    ShowDetails: bool
}

type Msg =
    | UpdateFilters of (SearchFilters -> SearchFilters)
    | Search
    | SearchResult of Result<SearchResult, string>
    | SelectCasualty of Casualty option
    | ClearSelection
    | GoToPage of (Model -> int)
    | ChangePageSize of int
    | LoadFilterOptions
    | FilterOptionsLoaded of ranks: string list * regions: string list
    | SetStatus of string
    | ClearStatus
    | ToggleDetails
    | ExportResults
    | ExportCompleted of bool * string

// ============================================================================
// РАБОТА С БАЗОЙ ДАННЫХ
// ============================================================================

module Database =
    let connectionString = "Data Source=casualties.db"
    
    let getConnection() = 
        let conn = new SqliteConnection(connectionString)
        let culture = Globalization.CultureInfo "uk-UA"
        conn.CreateCollation("UKR", fun x y -> 
            String.Compare(x, y, culture, Globalization.CompareOptions.IgnoreCase))
        conn

    let parseDateTime (value: obj) =
        if value = DBNull.Value then None
        else
            match value with
            | :? string as s -> 
                match DateTime.TryParse(s) with
                | true, dt -> Some dt
                | _ -> None
            | :? DateTime as dt -> Some dt
            | _ -> None
    
    let parseInt (value: obj) =
        if value = DBNull.Value then None
        else
            match value with
            | :? int64 as i -> Some (int i)
            | :? int as i -> Some i
            | :? string as s ->
                match Int32.TryParse(s) with
                | true, i -> Some i
                | _ -> None
            | _ -> None
    
    let getString (value: obj) =
        if value = DBNull.Value then None
        else Some (value.ToString())
    
    let getBool (value: obj) =
        if value = DBNull.Value then false
        else
            match value with
            | :? int64 as i -> i = 1L
            | :? int as i -> i = 1
            | :? bool as b -> b
            | :? string as s -> s = "1" || s.ToLower() = "true"
            | _ -> false
    
    let getSources (casualtyId: int64) (connection: SqliteConnection) =
        try
            let cmd = connection.CreateCommand()
            cmd.CommandText <- """
                SELECT s.url 
                FROM Sources s
                JOIN CasualtiesSources cs ON s.id = cs.source_id
                WHERE cs.casualty_id = @casualty_id
                ORDER BY s.url
            """
            cmd.Parameters.AddWithValue("@casualty_id", casualtyId) |> ignore
            
            use reader = cmd.ExecuteReader()
            let sources = List<string>()
            while reader.Read() do
                sources.Add(reader.GetString(0))
            List.ofSeq sources
        with _ -> []
    
    let search (filters: SearchFilters) : Async<Result<SearchResult,string>> =
        async {
            try
                use connection = getConnection()
                connection.Open()
                
                let conditions = List<string>()
                let parameters = Dictionary<string, obj>()
                
                let baseQuery = """
                    SELECT 
                        c.id, c.fullname, c.alt_fullname, c.age, c.sex,
                        c.date_of_birth, c.date_of_death, c.date_of_conscription,
                        c.date_of_funeral, c.is_died_from_wounds, c.is_from_decrees,
                        c.death_at, c.days_from_conscription,
                        r.name as rank_name,
                        cons.name as conscription_name,
                        rl.name as region_live,
                        rd.name as region_death,
                        rf.name as region_funeral
                    FROM Casualties c
                    LEFT JOIN Ranks r ON c.rank_id = r.id
                    LEFT JOIN Conscriptions cons ON c.conscription_id = cons.id
                    LEFT JOIN Regions rl ON c.region_of_live_id = rl.id
                    LEFT JOIN Regions rd ON c.region_of_death_id = rd.id
                    LEFT JOIN Regions rf ON c.region_of_funeral_id = rf.id
                    WHERE 1=1
                """
                
                if not (String.IsNullOrWhiteSpace filters.Fullname) then
                    conditions.Add "(c.fullname LIKE @fullname1 OR c.fullname LIKE @fullname2)"
                    parameters.Add("@fullname1", $"{filters.Fullname.ToUpper()}%%" )
                    parameters.Add("@fullname2", $"%%{filters.Fullname}%%")                    
                
                match filters.Rank with
                | Some rank when not (String.IsNullOrWhiteSpace rank) ->
                    conditions.Add("r.name = @rank")
                    parameters.Add("@rank", rank)
                | _ -> ()
                
                match filters.Region with
                | Some region when not (String.IsNullOrWhiteSpace region) ->
                    conditions.Add("(rl.name = @region OR rd.name = @region OR rf.name = @region)")
                    parameters.Add("@region", region)
                | _ -> ()
                
                match filters.Sex with
                | Some sex when not (String.IsNullOrWhiteSpace sex) ->
                    conditions.Add("c.sex = @sex")
                    parameters.Add("@sex", sex)
                | _ -> ()
                
                match filters.AgeFrom with
                | Some age ->
                    conditions.Add("c.age >= @age_from")
                    parameters.Add("@age_from", age)
                | _ -> ()
                
                match filters.AgeTo with
                | Some age ->
                    conditions.Add("c.age <= @age_to")
                    parameters.Add("@age_to", age)
                | _ -> ()
                
                match filters.DateOfBirthFrom with
                | Some date ->
                    conditions.Add("c.date_of_birth >= @dob_from")
                    parameters.Add("@dob_from", date.ToString("yyyy-MM-dd"))
                | _ -> ()

                match filters.DateOfBirthTo with
                | Some date ->
                    conditions.Add("c.date_of_birth <= @dob_to")
                    parameters.Add("@dob_to", date.ToString("yyyy-MM-dd"))
                | _ -> ()

                match filters.DateOfDeathFrom with
                | Some date ->
                    conditions.Add("c.date_of_death >= @dod_from")
                    parameters.Add("@dod_from", date.ToString("yyyy-MM-dd"))
                | _ -> ()

                match filters.DateOfDeathTo with
                | Some date ->
                    conditions.Add("c.date_of_death <= @dod_to")
                    parameters.Add("@dod_to", date.ToString("yyyy-MM-dd"))
                | _ -> ()
                
                match filters.IsDiedFromWounds with
                | Some true -> conditions.Add("c.is_died_from_wounds = 1")
                | Some false -> conditions.Add("c.is_died_from_wounds = 0")
                | _ -> ()
                
                match filters.IsFromDecrees with
                | Some true -> conditions.Add("c.is_from_decrees = 1")
                | Some false -> conditions.Add("c.is_from_decrees = 0")
                | _ -> ()
                
                let whereClause = 
                    if conditions.Count > 0 then
                        " AND " + String.Join(" AND ", conditions)
                    else ""
                
                let countQuery = sprintf """
                    SELECT COUNT(*) 
                    FROM Casualties c
                    LEFT JOIN Ranks r ON c.rank_id = r.id
                    LEFT JOIN Conscriptions cons ON c.conscription_id = cons.id
                    LEFT JOIN Regions rl ON c.region_of_live_id = rl.id
                    LEFT JOIN Regions rd ON c.region_of_death_id = rd.id
                    LEFT JOIN Regions rf ON c.region_of_funeral_id = rf.id
                    WHERE 1=1 %s""" whereClause
                
                let dataQuery = sprintf """
                    %s %s
                    ORDER BY c.fullname COLLATE UKR
                    LIMIT @limit OFFSET @offset""" baseQuery whereClause
                
                use countCmd = connection.CreateCommand()
                countCmd.CommandText <- countQuery
                for kv in parameters do
                    countCmd.Parameters.AddWithValue(kv.Key, kv.Value) |> ignore
                
                let totalCount = countCmd.ExecuteScalar() :?> int64
                
                use dataCmd = connection.CreateCommand()
                dataCmd.CommandText <- dataQuery
                for kv in parameters do
                    dataCmd.Parameters.AddWithValue(kv.Key, kv.Value) |> ignore
                dataCmd.Parameters.AddWithValue("@limit", filters.Limit) |> ignore
                dataCmd.Parameters.AddWithValue("@offset", filters.Offset) |> ignore
                
                use reader = dataCmd.ExecuteReader()
                let results = List<Casualty>()
                
                while reader.Read() do
                    let id = reader.GetInt64(0)
                    let casualty = {
                        Id = id
                        Fullname = reader.GetString(1)
                        AltFullname = getString (reader.GetValue(2))
                        Age = parseInt (reader.GetValue(3))
                        Sex = getString (reader.GetValue(4))
                        DateOfBirth = parseDateTime (reader.GetValue(5))
                        DateOfDeath = parseDateTime (reader.GetValue(6))
                        DateOfConscription = parseDateTime (reader.GetValue(7))
                        DateOfFuneral = parseDateTime (reader.GetValue(8))
                        IsDiedFromWounds = getBool (reader.GetValue(9))
                        IsFromDecrees = getBool (reader.GetValue(10))
                        DeathAt = parseDateTime (reader.GetValue(11))
                        DaysFromConscription = parseInt (reader.GetValue(12))
                        Rank = getString (reader.GetValue(13))
                        Conscription = getString (reader.GetValue(14))
                        RegionOfLive = getString (reader.GetValue(15))
                        RegionOfDeath = getString (reader.GetValue(16))
                        RegionOfFuneral = getString (reader.GetValue(17))
                        Sources = getSources id connection
                    }
                    results.Add casualty
                
                return Ok {
                    Items = List.ofSeq results
                    TotalCount = int totalCount
                    Page = filters.Offset / filters.Limit + 1
                    TotalPages = int ((totalCount + int64 filters.Limit - 1L) / int64 filters.Limit)
                }
            with ex ->
                return Error ex.Message
        }
    
    let getFilterOptions () =
        async {
            try
                use connection = getConnection()
                connection.Open()
                
                let ranks = List<string>()
                try
                    let ranksCmd = connection.CreateCommand()
                    ranksCmd.CommandText <- "SELECT DISTINCT name FROM Ranks ORDER BY name"
                    use ranksReader = ranksCmd.ExecuteReader()
                    while ranksReader.Read() do
                        ranks.Add(ranksReader.GetString(0))
                with _ -> ()
                
                let regions = List<string>()
                try
                    let regionsCmd = connection.CreateCommand()
                    regionsCmd.CommandText <- "SELECT DISTINCT name FROM Regions ORDER BY name"
                    use regionsReader = regionsCmd.ExecuteReader()
                    while regionsReader.Read() do
                        regions.Add(regionsReader.GetString(0))
                with _ -> ()
                
                return (List.ofSeq ranks, List.ofSeq regions)
            with _ ->
                return ([], [])
        }
    
    let exportToCsv (results: Casualty list) (filePath: string) =
        async {
            try
                use writer = new StreamWriter(filePath)
                writer.WriteLine "ФИО,Звание,Возраст,Пол,Дата рождения,Дата смерти,Регион жизни,Регион смерти,Источники"
                
                for c in results do
                    let fields = [
                        c.Fullname
                        defaultArg c.Rank ""
                        defaultArg (c.Age |> Option.map string) ""
                        defaultArg c.Sex ""
                        defaultArg (c.DateOfBirth |> Option.map (fun d -> d.ToString("dd.MM.yyyy"))) ""
                        defaultArg (c.DateOfDeath |> Option.map (fun d -> d.ToString("dd.MM.yyyy"))) ""
                        defaultArg c.RegionOfLive ""
                        defaultArg c.RegionOfDeath ""
                        String.Join("; ", c.Sources)
                    ]
                    writer.WriteLine(String.Join(",", fields))
                
                return true
            with ex ->
                return false
        }
    
    let checkDatabaseExists () =
        try
            use connection = getConnection()
            connection.Open()
            let cmd = connection.CreateCommand()
            cmd.CommandText <- "SELECT COUNT(*) FROM Casualties"
            let count = cmd.ExecuteScalar() :?> int64
            count > 0L
        with _ -> false

// ============================================================================
// ОСНОВНОЙ МОДУЛЬ ПРИЛОЖЕНИЯ
// ============================================================================
module Application =    
    let init () : Model * Elmish.Cmd<Msg> =
        let initialFilters = {
            Fullname = ""
            Rank = None
            Region = None
            AgeFrom = None
            AgeTo = None
            DateOfBirthFrom = None
            DateOfBirthTo = None
            DateOfDeathFrom = None
            DateOfDeathTo = None
            Sex = None
            IsDiedFromWounds = None
            IsFromDecrees = None
            Limit = 50
            Offset = 0
        }
        
        let model = {
            Filters = initialFilters
            Results = []
            TotalCount = 0
            CurrentPage = 1
            PageSize = 50
            IsLoading = false
            StatusMessage = "Готов к работе"
            SelectedCasualty = None
            AvailableRanks = []
            AvailableRegions = []
            AvailableSex = ["m"; "f"]
            ShowDetails = false
        }
        
        let loadOptionsCmd =
            Elmish.Cmd.OfAsync.either Database.getFilterOptions () FilterOptionsLoaded (fun ex -> SetStatus (sprintf "Ошибка: %s" ex.Message))
        
        let searchCmd = 
            Elmish.Cmd.OfAsync.either Database.search initialFilters SearchResult (fun ex -> SetStatus (sprintf "Ошибка: %s" ex.Message))
        
        model, Elmish.Cmd.batch [ loadOptionsCmd; searchCmd ]
    
    let update (msg: Msg) (model: Model) : Model * Elmish.Cmd<Msg> =
        match msg with
        | UpdateFilters updateFn ->
            let newFilters = updateFn model.Filters
            { model with Filters = newFilters }, Elmish.Cmd.none
        
        | Search ->
            let filters = { model.Filters with Offset = (model.CurrentPage - 1) * model.PageSize; Limit = model.PageSize }
            { model with IsLoading = true; StatusMessage = "Поиск..."},
            Elmish.Cmd.OfAsync.either Database.search filters SearchResult (fun ex -> SetStatus (sprintf "Ошибка: %s" ex.Message))
        
        | SearchResult result ->
            match result with
            | Ok data ->
                { model with 
                    Results = data.Items
                    TotalCount = data.TotalCount
                    CurrentPage = data.Page
                    IsLoading = false
                    StatusMessage = sprintf "Найдено %d записей" data.TotalCount },
                Elmish.Cmd.none
            | Error err ->
                { model with 
                    IsLoading = false
                    StatusMessage = sprintf "Ошибка: %s" err },
                Elmish.Cmd.none
        
        | SelectCasualty casualty ->
            { model with 
                SelectedCasualty = casualty
                ShowDetails = casualty.IsSome },
                Elmish.Cmd.none
        
        | ClearSelection ->
            { model with SelectedCasualty = None; ShowDetails = false }, Elmish.Cmd.none
        
        | GoToPage pageFn ->
            let page = pageFn model
            let newOffset = (page - 1) * model.PageSize
            let filters = { model.Filters with Offset = newOffset; Limit = model.PageSize }
            { model with 
                CurrentPage = page
                Filters = filters
                IsLoading = true 
                StatusMessage = sprintf "Загрузка страницы %d..." page },
            Elmish.Cmd.OfAsync.either Database.search filters SearchResult (fun ex -> SetStatus (sprintf "Ошибка: %s" ex.Message))
        
        | ChangePageSize size ->
            let filters = { model.Filters with Limit = size; Offset = 0 }
            { model with 
                PageSize = size
                CurrentPage = 1
                Filters = filters
                IsLoading = true 
                StatusMessage = sprintf "Смена размера страницы на %d" size },
            Elmish.Cmd.OfAsync.either Database.search filters SearchResult (fun ex -> SetStatus (sprintf "Ошибка: %s" ex.Message))
        
        | LoadFilterOptions ->
            model, Elmish.Cmd.OfAsync.either Database.getFilterOptions () FilterOptionsLoaded (fun ex -> SetStatus (sprintf "Ошибка: %s" ex.Message))
        
        | FilterOptionsLoaded (ranks, regions) ->
            { model with 
                AvailableRanks = ranks
                AvailableRegions = regions },
            Elmish.Cmd.none
        
        | SetStatus msg ->
            { model with StatusMessage = msg }, Elmish.Cmd.none
        
        | ClearStatus ->
            { model with StatusMessage = "" }, Elmish.Cmd.none
        
        | ToggleDetails ->
            { model with ShowDetails = not model.ShowDetails }, Elmish.Cmd.none
        
        | ExportResults ->
            let filePath = sprintf "export_%s.csv" (DateTime.Now.ToString "yyyyMMdd_HHmmss")
            model, Elmish.Cmd.OfAsync.either (Database.exportToCsv model.Results) filePath (fun success -> ExportCompleted (success, filePath)) (fun ex -> SetStatus (sprintf "Ошибка экспорта: %s" ex.Message))
        
        | ExportCompleted (success, message) ->
            { model with StatusMessage = if success then "Экспорт завершен: " + message else "Ошибка экспорта" },
            Elmish.Cmd.none
// ============================================================================
// VIEWS
// ============================================================================

module Views =
    open Avalonia.Controls.Primitives
    open Avalonia.Data
    
    let private dateTimeToString (dt: DateTime option) =
        match dt with
        | Some d -> d.ToString "dd.MM.yyyy"
        | None -> ""
    
    let view (model: Model) (dispatch: Msg -> unit) =
        DockPanel.create [
            DockPanel.children [
                // Верхняя панель
                StackPanel.create [
                    StackPanel.dock Dock.Top
                    StackPanel.margin 10
                    StackPanel.spacing 10
                    StackPanel.children [
                        // Заголовок
                        TextBlock.create [
                            TextBlock.text "Поиск по базе данных потерь"
                            TextBlock.fontSize 20
                            TextBlock.fontWeight FontWeight.Bold
                        ]
                        
                        // Строка поиска
                        Border.create [
                            Border.cornerRadius 4
                            
                            Border.borderBrush Brushes.Gray
                            Border.borderThickness 1
                            Border.padding 5
                            Border.child (
                                StackPanel.create [
                                    StackPanel.orientation Orientation.Horizontal
                                    StackPanel.spacing 10
                                    StackPanel.children [
                                        TextBox.create [
                                            TextBox.placeHolderText "Введите ФИО..."
                                            TextBox.text model.Filters.Fullname
                                            TextBox.width 300
                                            TextBox.onTextChanged (fun text ->
                                                dispatch (UpdateFilters (fun filters -> { filters with Fullname = text }))
                                            )
                                        ]
                                        Button.create [
                                            Button.content "🔍 Найти"
                                            Button.isDefault true
                                            Button.onClick (fun _ -> dispatch Search)
                                            Button.classes ["primary"]
                                        ]
                                        Button.create [
                                            Button.content "📊 Экспорт"
                                            Button.onClick (fun _ -> dispatch ExportResults)
                                            Button.tip "Экспортировать результаты в CSV"
                                        ]
                                        Button.create [
                                            Button.content "🔄 Сброс"
                                            Button.onClick (fun _ ->
                                                dispatch (UpdateFilters (fun filters -> { 
                                                    filters with
                                                        Fullname = ""
                                                        Rank = None
                                                        Region = None
                                                        AgeFrom = None
                                                        AgeTo = None
                                                        DateOfBirthFrom = None
                                                        DateOfBirthTo = None
                                                        DateOfDeathFrom = None
                                                        DateOfDeathTo = None
                                                        Sex = None
                                                        IsDiedFromWounds = None
                                                        IsFromDecrees = None
                                                    }))
                                                dispatch Search
                                            )
                                        ]
                                    ]
                                ]
                            )
                        ]
                        
                        // Фильтры
                        Border.create [
                            Border.cornerRadius 4
                            Border.padding 10
                            Border.child (
                                StackPanel.create [
                                    StackPanel.spacing 10
                                    StackPanel.children [
                                        // Первая строка фильтров
                                        WrapPanel.create [
                                            WrapPanel.itemSpacing 10
                                            WrapPanel.children [
                                                ComboBox.create [
                                                    ComboBox.placeholderText "Звание"
                                                    ComboBox.width 150
                                                    ComboBox.dataItems (List.append [""] model.AvailableRanks)
                                                    ComboBox.selectedItem (
                                                        match model.Filters.Rank with
                                                        | Some rank -> rank
                                                        | None -> ""
                                                    )
                                                    ComboBox.onSelectedItemChanged (fun item ->
                                                        let rank = 
                                                            match item with
                                                            | :? string as s when not (String.IsNullOrWhiteSpace s) -> Some s
                                                            | _ -> None
                                                        dispatch (UpdateFilters (fun filters -> { filters with Rank = rank }))
                                                    )
                                                ]
                                                
                                                ComboBox.create [
                                                    ComboBox.placeholderText "Регион"
                                                    ComboBox.width 150
                                                    ComboBox.dataItems (List.append [""] model.AvailableRegions)
                                                    ComboBox.selectedItem (
                                                        match model.Filters.Region with
                                                        | Some region -> region
                                                        | None -> ""
                                                    )
                                                    ComboBox.onSelectedItemChanged (fun item ->
                                                        let region = 
                                                            match item with
                                                            | :? string as s when not (String.IsNullOrWhiteSpace s) -> Some s
                                                            | _ -> None
                                                        dispatch (UpdateFilters (fun filters -> { filters with Region = region }))
                                                    )
                                                ]
                                                
                                                ComboBox.create [
                                                    ComboBox.placeholderText "Пол"
                                                    ComboBox.width 100
                                                    ComboBox.dataItems ["Все"; "Мужской"; "Женский"]
                                                    ComboBox.selectedItem (
                                                        match model.Filters.Sex with
                                                        | Some "m" -> "Мужской"
                                                        | Some "f" -> "Женский"
                                                        | _ -> "Все"
                                                    )
                                                    ComboBox.onSelectedItemChanged (fun item ->
                                                        let sex = 
                                                            match item with
                                                            | :? string as s ->
                                                                match s with
                                                                | "Мужской" -> Some "m"
                                                                | "Женский" -> Some "f"
                                                                | _ -> None
                                                            | _ -> None
                                                        dispatch (UpdateFilters (fun filters -> { filters with Sex = sex }))
                                                    )
                                                ]
                                                
                                                // Возраст
                                                StackPanel.create [
                                                    StackPanel.orientation Orientation.Horizontal
                                                    StackPanel.spacing 5
                                                    StackPanel.children [
                                                        TextBlock.create [
                                                            TextBlock.text "Возраст:"
                                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                                        ]
                                                        NumericUpDown.create [
                                                            NumericUpDown.width 120
                                                            NumericUpDown.minimum 16
                                                            NumericUpDown.maximum 80
                                                            NumericUpDown.formatString "0"
                                                            NumericUpDown.value (
                                                                match model.Filters.AgeFrom with
                                                                | Some age -> Nullable (Convert.ToDecimal age)
                                                                | None -> Nullable()
                                                            )
                                                            NumericUpDown.onValueChanged (fun v ->
                                                                let age = if v.HasValue then Some (int v.Value) else None
                                                                dispatch (UpdateFilters (fun filters -> { filters with AgeFrom = age }))
                                                            )
                                                        ]
                                                        TextBlock.create [
                                                            TextBlock.text "-"
                                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                                        ]
                                                        NumericUpDown.create [
                                                            NumericUpDown.width 120
                                                            NumericUpDown.minimum 16
                                                            NumericUpDown.maximum 80
                                                            NumericUpDown.formatString "0"
                                                            NumericUpDown.value (
                                                                match model.Filters.AgeTo with
                                                                | Some age -> Nullable (Convert.ToDecimal age)
                                                                | None -> Nullable()
                                                            )
                                                            NumericUpDown.onValueChanged (fun v ->
                                                                let age = if v.HasValue then Some (int v.Value) else None
                                                                dispatch (UpdateFilters (fun filters -> { filters with AgeTo = age }))
                                                            )
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]   
                                        // Конвертер DateTimeOffset option -> Nullable<DateTimeOffset>
                                        let toNullable (dto: DateTimeOffset option) =
                                            match dto with
                                            | Some d -> Nullable d
                                            | None -> Nullable()
                                        
                                        // Конвертер Nullable<DateTimeOffset> -> DateTimeOffset option
                                        let fromNullable (dto: Nullable<DateTimeOffset>) =
                                            if dto.HasValue then Some dto.Value else None                                                                           
                                        // Вторая строка фильтров - даты
                                        WrapPanel.create [
                                            WrapPanel.itemSpacing 10
                                            WrapPanel.children [
                                                StackPanel.create [
                                                    StackPanel.orientation Orientation.Horizontal
                                                    StackPanel.spacing 5
                                                    StackPanel.children [
                                                        TextBlock.create [
                                                            TextBlock.text "Дата рождения от: "
                                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                                        ]
                                                        DatePicker.create [
                                                            DatePicker.width 120
                                                            DatePicker.selectedDate (toNullable model.Filters.DateOfBirthFrom)
                                                            DatePicker.onSelectedDateChanged (fun date ->
                                                                dispatch (UpdateFilters (fun filters -> { filters with DateOfBirthFrom = fromNullable date }))
                                                            )
                                                        ]
                                                        TextBlock.create [
                                                            TextBlock.text "- до "
                                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                                        ]
                                                        DatePicker.create [
                                                            DatePicker.width 120
                                                            DatePicker.selectedDate (toNullable model.Filters.DateOfBirthTo)
                                                            DatePicker.onSelectedDateChanged (fun date ->
                                                                dispatch (UpdateFilters (fun filters -> { filters with DateOfBirthTo = fromNullable date }))
                                                            )
                                                        ]
                                                    ]
                                                ]
                                                
                                                StackPanel.create [
                                                    StackPanel.orientation Orientation.Horizontal
                                                    StackPanel.spacing 5
                                                    StackPanel.children [
                                                        TextBlock.create [
                                                            TextBlock.text "Дата смерти от: "
                                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                                        ]
                                                        DatePicker.create [
                                                            DatePicker.width 120
                                                            DatePicker.selectedDate (toNullable model.Filters.DateOfDeathFrom)
                                                            DatePicker.onSelectedDateChanged (fun date ->
                                                                dispatch (UpdateFilters (fun filters -> { filters with DateOfDeathFrom = fromNullable date }))
                                                            )
                                                        ]
                                                        TextBlock.create [
                                                            TextBlock.text "- до "
                                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                                        ]
                                                        DatePicker.create [
                                                            DatePicker.width 120
                                                            DatePicker.selectedDate (toNullable model.Filters.DateOfDeathTo)
                                                            DatePicker.onSelectedDateChanged (fun date ->
                                                                dispatch (UpdateFilters (fun filters -> { filters with DateOfDeathTo = fromNullable date }))
                                                            )
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                        
                                        // Чекбоксы
                                        WrapPanel.create [
                                            WrapPanel.itemSpacing 20
                                            WrapPanel.children [
                                                CheckBox.create [
                                                    CheckBox.content "Умер от ран"
                                                    CheckBox.isChecked (
                                                        match model.Filters.IsDiedFromWounds with
                                                        | Some true -> Some true
                                                        | Some false -> Some false
                                                        | None -> None
                                                    )
                                                    CheckBox.onIsCheckedChanged (fun eventArgs ->
                                                        match eventArgs.Source with
                                                        | :? CheckBox as cb -> 
                                                            let currentChecked = cb.IsChecked |> Option.ofNullable
                                                            dispatch (UpdateFilters (fun filters -> { filters with IsDiedFromWounds = currentChecked }))
                                                        | _ -> ()
                                                    )
                                                ]
                                                CheckBox.create [
                                                    CheckBox.content "Из указов"
                                                    CheckBox.isChecked (
                                                        match model.Filters.IsFromDecrees with
                                                        | Some true -> Some true
                                                        | Some false -> Some false
                                                        | None -> None
                                                    )
                                                    CheckBox.onIsCheckedChanged (fun eventArgs ->
                                                        match eventArgs.Source with
                                                        | :? CheckBox as cb -> 
                                                            let currentChecked = cb.IsChecked |> Option.ofNullable
                                                            dispatch (UpdateFilters (fun filters -> { filters with IsFromDecrees = currentChecked }))
                                                        | _ -> ()
                                                    )
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            )
                        ]
                        
                        // Статус
                        Border.create [
                            Border.cornerRadius 4
                            Border.background (
                                if model.StatusMessage.Contains "Ошибка" then Brushes.IndianRed
                                elif model.StatusMessage.Contains "Найдено" then Brushes.LightGreen
                                else Brushes.LightGray
                            )
                            Border.padding 5
                            Border.child (
                                TextBlock.create [
                                    TextBlock.text model.StatusMessage
                                    TextBlock.fontWeight FontWeight.SemiBold
                                ]
                            )
                        ]
                    ]
                ]
                
                // Основной контент
                Grid.create [
                    Grid.rowDefinitions "*,Auto"
                    Grid.children [
                        // Таблица результатов
                        DataGrid.create [
                            DataGrid.items model.Results
                            DataGrid.canUserSortColumns false
                            DataGrid.autoGeneratedColumns false
                            DataGrid.canUserResizeColumns true
                            DataGrid.isReadOnly true
                            DataGrid.row 0
                            DataGrid.onSelectedItemChanged (fun item ->
                                match item with
                                | :? Casualty as c -> dispatch (SelectCasualty (Some c))
                                | _ -> ()
                            )
                            DataGrid.columns [
                                DataGridTextColumn.create [
                                    DataGridTextColumn.header "ФИО"
                                    DataGridTextColumn.binding (Binding "Fullname")
                                    DataGridTextColumn.width (DataGridLength 250)
                                ]
                                DataGridTemplateColumn.create [
                                    DataGridTemplateColumn.header "Звание"
                                    DataGridTemplateColumn.cellTemplate (
                                        DataTemplateView<_>.create (fun (data: Casualty) ->
                                            TextBlock.create [
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                TextBlock.text (match data.Rank with 
                                                                | Some str -> str
                                                                | _ -> "")
                                            ]
                                        )
                                    )                                  
                                    DataGridTemplateColumn.width (DataGridLength 120)
                                ]
                                DataGridTemplateColumn.create [
                                    DataGridTemplateColumn.header "Возраст"
                                    DataGridTemplateColumn.cellTemplate (
                                        DataTemplateView<_>.create (fun (data: Casualty) ->
                                            TextBlock.create [
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                TextBlock.text (match data.Age with 
                                                                | Some str -> str.ToString()
                                                                | _ -> "")
                                            ]
                                        )
                                    )                                     
                                    DataGridTemplateColumn.width (DataGridLength 70)
                                ]
                                DataGridTemplateColumn.create [
                                    DataGridTemplateColumn.header "Пол"
                                    DataGridTemplateColumn.cellTemplate (
                                        DataTemplateView<_>.create (fun (data: Casualty) ->
                                            TextBlock.create [
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                TextBlock.text (match data.Sex with 
                                                                | Some str -> if str = "m" then "м" else "ж"
                                                                | _ -> "")
                                            ]
                                        )
                                    )                                      
                                    DataGridTemplateColumn.width (DataGridLength 60)
                                ]
                                DataGridTemplateColumn.create [
                                    DataGridTemplateColumn.header "Дата рождения"
                                    DataGridTemplateColumn.cellTemplate (
                                        DataTemplateView<_>.create (fun (data: Casualty) ->
                                            TextBlock.create [
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                TextBlock.text (match data.DateOfBirth with 
                                                                | Some dt -> dt.ToString "dd.MM.yyyy"
                                                                | _ -> "")
                                            ]
                                        )
                                    )
                                    DataGridTemplateColumn.width (DataGridLength 110)
                                ]
                                DataGridTemplateColumn.create [
                                    DataGridTemplateColumn.header "Дата смерти"
                                    DataGridTemplateColumn.cellTemplate (
                                        DataTemplateView<_>.create (fun (data: Casualty) ->
                                            TextBlock.create [
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                TextBlock.text (match data.DateOfDeath with 
                                                                | Some dt -> dt.ToString "dd.MM.yyyy"
                                                                | _ -> "")
                                            ]
                                        )
                                    )
                                    DataGridTemplateColumn.width (DataGridLength 110)
                                ]                                
                                DataGridTemplateColumn.create [
                                    DataGridTemplateColumn.header "Регион гибели"
                                    DataGridTemplateColumn.cellTemplate (
                                        DataTemplateView<_>.create (fun (data: Casualty) ->
                                            TextBlock.create [
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                TextBlock.text (match data.RegionOfDeath with 
                                                                | Some str -> str
                                                                | _ -> "")
                                            ]
                                        )
                                    )                                     
                                    DataGridTemplateColumn.width (DataGridLength 180)
                                ]
                                DataGridTemplateColumn.create [
                                    DataGridTemplateColumn.header "Источники"
                                    DataGridTemplateColumn.cellTemplate (
                                        DataTemplateView<_>.create (fun (data: Casualty) ->
                                            StackPanel.create [
                                                StackPanel.orientation Orientation.Horizontal
                                                StackPanel.verticalAlignment VerticalAlignment.Center
                                                StackPanel.spacing 2
                                                StackPanel.children [
                                                    for  (i, url) in Seq.indexed data.Sources do
                                                        match Uri.TryCreate(url, UriKind.Absolute) with
                                                        | true, uri -> 
                                                            HyperlinkButton.create [
                                                                HyperlinkButton.navigateUri uri
                                                                HyperlinkButton.tip url
                                                                HyperlinkButton.content (i+1)
                                                            ]
                                                        | false, _  -> ()
                                                ]
                                            ]
                                        )
                                    )
                                    DataGridTemplateColumn.width (DataGridLength 200)
                                ]                                 
                            ]
                        ]
                        
                        // Пагинация
                        StackPanel.create [
                            StackPanel.row 1
                            StackPanel.orientation Orientation.Horizontal
                            StackPanel.horizontalAlignment HorizontalAlignment.Center
                            StackPanel.margin 10
                            StackPanel.spacing 10
                            StackPanel.children [
                                Button.create [
                                    Button.content "⏮"
                                    Button.tip "Первая страница"
                                    Button.isEnabled (model.CurrentPage > 1)
                                    Button.onClick (fun _ -> dispatch (GoToPage (fun _ -> 1)))
                                ]
                                Button.create [
                                    Button.content "◀"
                                    Button.tip "Предыдущая страница"
                                    Button.isEnabled (model.CurrentPage > 1)
                                    Button.onClick (fun _ -> dispatch (GoToPage (fun model -> model.CurrentPage - 1)))
                                ]
                                
                                TextBlock.create [
                                    TextBlock.text (
                                        let totalPages = max 1 ((model.TotalCount + model.PageSize - 1) / model.PageSize)
                                        sprintf "Страница %d из %d" model.CurrentPage totalPages
                                    )
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                    TextBlock.fontWeight FontWeight.Bold
                                ]
                                
                                Button.create [
                                    Button.content "▶"
                                    Button.tip "Следующая страница"
                                    Button.isEnabled (
                                        let totalPages = (model.TotalCount + model.PageSize - 1) / model.PageSize
                                        model.CurrentPage < totalPages
                                    )
                                    Button.onClick (fun _ -> dispatch (GoToPage (fun model -> model.CurrentPage + 1)))
                                ]
                                Button.create [
                                    Button.content "⏭"
                                    Button.tip "Последняя страница"
                                    Button.isEnabled (
                                        let totalPages = (model.TotalCount + model.PageSize - 1) / model.PageSize
                                        model.CurrentPage < totalPages
                                    )
                                    Button.onClick (fun _ -> 
                                        dispatch (GoToPage (fun model -> 
                                            let totalPages = max 1 ((model.TotalCount + model.PageSize - 1) / model.PageSize)
                                            totalPages
                                        ))
                                    )
                                ]
                                
                                ComboBox.create [
                                    ComboBox.width 80
                                    ComboBox.dataItems ["10"; "20"; "50"; "100"; "200"; "500"]
                                    ComboBox.selectedItem (model.PageSize.ToString())
                                    ComboBox.onSelectedItemChanged (fun item ->
                                        match item with
                                        | :? string as s ->
                                            match System.Int32.TryParse(s) with
                                            | true, size -> dispatch (ChangePageSize size)
                                            | _ -> ()
                                        | _ -> ()
                                    )
                                ]
                                
                                TextBlock.create [
                                    TextBlock.text (sprintf "Всего: %d записей" model.TotalCount)
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                    TextBlock.margin (Thickness(20, 0, 0, 0))
                                    TextBlock.fontWeight FontWeight.SemiBold
                                ]
                                
                                if model.IsLoading then
                                    ProgressBar.create [
                                        ProgressBar.width 100
                                        ProgressBar.height 20
                                        ProgressBar.isIndeterminate true
                                    ]
                            ]
                        ]
                    ]
                ]
                
                // Детали выбранной записи (боковая панель)
                if model.ShowDetails && model.SelectedCasualty.IsSome then
                    let c = model.SelectedCasualty.Value
                    Border.create [
                        Border.dock Dock.Right
                        Border.width 320
                        Border.borderBrush Brushes.Gray
                        Border.background Brushes.Black
                        Border.borderThickness (Thickness(1, 0, 0, 0))
                        Border.padding 15
                        Border.child (
                            ScrollViewer.create [
                                ScrollViewer.content (
                                    StackPanel.create [
                                        StackPanel.spacing 10
                                        StackPanel.children [
                                            // Заголовок с кнопкой закрытия
                                            Grid.create [
                                                Grid.columnDefinitions "auto,*,auto"
                                                Grid.children [
                                                    TextBlock.create [
                                                        Grid.column 0
                                                        TextBlock.text "Детали записи"
                                                        TextBlock.fontSize 16
                                                        TextBlock.fontWeight FontWeight.Bold
                                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                                    ]
                                                    Button.create [
                                                        Grid.column 2
                                                        Button.content "✕"
                                                        Button.horizontalAlignment HorizontalAlignment.Right
                                                        Button.onClick (fun _ -> dispatch ClearSelection)
                                                        Button.classes ["close-button"]
                                                    ]                                                    
                                                ]
                                            ]
                                            Border.create [
                                                Border.cornerRadius 4
                                                //Border.background Brushes.DarkGray
                                                Border.padding 10
                                                Border.child (
                                                    StackPanel.create [
                                                        StackPanel.spacing 5
                                                        StackPanel.children [
                                                            TextBlock.create [
                                                                TextBlock.text c.Fullname
                                                                TextBlock.fontSize 18
                                                                TextBlock.fontWeight FontWeight.Bold
                                                            ]
                                                            if c.AltFullname.IsSome then
                                                                TextBlock.create [
                                                                    TextBlock.text (sprintf "Альт. имя: %s" c.AltFullname.Value)
                                                                    TextBlock.fontStyle FontStyle.Italic
                                                                ]
                                                            TextBlock.create [
                                                                TextBlock.text (sprintf "Звание: %s" (defaultArg c.Rank "Не указано"))
                                                            ]
                                                            TextBlock.create [
                                                                TextBlock.text (sprintf "Возраст: %s" (defaultArg (c.Age |> Option.map string) "Не указан"))
                                                            ]
                                                            TextBlock.create [
                                                                TextBlock.text (sprintf "Пол: %s" (defaultArg c.Sex "Не указан"))
                                                            ]
                                                            TextBlock.create [
                                                                TextBlock.text (sprintf "Дата рождения: %s" (dateTimeToString c.DateOfBirth))
                                                            ]
                                                            TextBlock.create [
                                                                TextBlock.text (sprintf "Дата смерти: %s" (dateTimeToString c.DateOfDeath))
                                                            ]
                                                            TextBlock.create [
                                                                TextBlock.text (sprintf "Регион жизни: %s" (defaultArg c.RegionOfLive "Не указан"))
                                                            ]
                                                            TextBlock.create [
                                                                TextBlock.text (sprintf "Регион смерти: %s" (defaultArg c.RegionOfDeath "Не указан"))
                                                            ]
                                                            TextBlock.create [
                                                                TextBlock.text (sprintf "Умер от ран: %s" (if c.IsDiedFromWounds then "Да" else "Нет"))
                                                            ]
                                                            TextBlock.create [
                                                                TextBlock.text (sprintf "Из указов: %s" (if c.IsFromDecrees then "Да" else "Нет"))
                                                            ]
                                                            
                                                            if not (List.isEmpty c.Sources) then
                                                                StackPanel.create [
                                                                    StackPanel.children [
                                                                        TextBlock.create [
                                                                            TextBlock.text "Источники:"
                                                                            TextBlock.fontWeight FontWeight.Bold
                                                                            TextBlock.margin (Thickness(0, 5, 0, 0))
                                                                        ]                                                                        
                                                                        for  (i, url) in Seq.indexed c.Sources do
                                                                            match Uri.TryCreate(url, UriKind.Absolute) with
                                                                            | true, uri -> 
                                                                                HyperlinkButton.create [
                                                                                    HyperlinkButton.navigateUri uri
                                                                                    HyperlinkButton.tip url
                                                                                    HyperlinkButton.content url
                                                                                ]
                                                                            | false, _  -> ()                                                                       
                                                                    ]
                                                                ]
                                                        ]
                                                    ]
                                                )
                                            ]
                                        ]
                                    ]
                                )
                            ]
                        )
                    ]
            ]
        ]

// ============================================================================
// ГЛАВНОЕ ОКНО
// ============================================================================
type MainWindow() as this =
    inherit HostWindow()
    do
        base.Title <- "Поиск по базе данных потерь"
        base.Width <- 1400.0
        base.Height <- 900.0
        base.MinWidth <- 900.0
        base.MinHeight <- 600.0
        
        let dbExists = Database.checkDatabaseExists()
        if not dbExists then
            this.Title <- "Поиск по базе данных потерь - БД не найдена"        
        
        let updateFn = Application.update
        let viewFn = Views.view
        let initFn = Application.init
        
        Elmish.Program.mkProgram initFn updateFn viewFn
        |> Elmish.Program.withHost this
        |> Elmish.Program.runWithAvaloniaSyncDispatch()

// ============================================================================
// ЗАПУСК ПРИЛОЖЕНИЯ
// ============================================================================

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add ( Themes.Fluent.FluentTheme() )
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark
        this.Styles.Load "avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"

    override this.OnFrameworkInitializationCompleted() =

        match this.ApplicationLifetime with
        | :? ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.MainWindow <- MainWindow()
            printfn "App running..."
        | _ -> ()   

let app =
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime([||])    
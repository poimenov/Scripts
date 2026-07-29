#r "nuget: FSharp.Data, 8.1.14"
#r "nuget: Microsoft.Data.Sqlite, 10.0.10"

open System
open System.Threading.Tasks
open System.Collections.Concurrent
open System.Threading
open Microsoft.Data.Sqlite
open FSharp.Data


[<Literal>]
let sample =
    """
{
  "items": [
    {
      "fullname": "Фамилия Имя Отчество",
      "alt_fullname": "",
      "rank": "",
      "conscription": "",
      "days_from_conscription": "",
      "age": "",
      "sex": "",
      "date_of_birth": "",
      "date_of_death": "",
      "date_of_conscription": "",
      "date_of_funeral": "",
      "is_died_from_wounds": "",
      "is_from_decrees": "",
      "sources": "",
      "death_at": "",
      "region_of_live": "",
      "region_of_death": "",
      "region_of_funeral": ""
    },
    {
      "fullname": "Фамилия Имя Отчество",
      "alt_fullname": null,
      "rank": null,
      "conscription": null,
      "days_from_conscription": null,
      "age": null,
      "sex": null,
      "date_of_birth": null,
      "date_of_death": null,
      "date_of_conscription": null,
      "date_of_funeral": null,
      "is_died_from_wounds": null,
      "is_from_decrees": null,
      "sources": null,
      "death_at": null,
      "region_of_live": null,
      "region_of_death": null,
      "region_of_funeral": null
    }    
 ],
  "paginator": {
    "on_page": 200,
    "per_page": 200,
    "page": 1,
    "pages": 11,
    "total": 2071
  }
}        
    """

// Определяем тип для элементов
type Item = {
    Fullname: string
    AltFullname: string option
    Rank: string option
    Conscription: string option
    DaysFromConscription: string option
    Age: string option
    Sex: string option
    DateOfBirth: string option
    DateOfDeath: string option
    DateOfConscription: string option
    DateOfFuneral: string option
    IsDiedFromWounds: string option
    IsFromDecrees: string option
    Sources: string option
    DeathAt: string option
    RegionOfLive: string option
    RegionOfDeath: string option
    RegionOfFuneral: string option
}

type Paginator = {
    OnPage: int
    PerPage: int
    Page: int
    Pages: int
    Total: int
}

type Response = {
    Items: Item[]
    Paginator: Paginator
}

// Парсинг JSON с помощью JsonProvider
type Ukr200Provider =
    JsonProvider<
        sample,
        SampleIsList=false,
        RootName="Page",
        Encoding="utf-8"
    >

// Глобальный объект для синхронизации записи в файл
let errorLock = obj()

// Функция для записи ошибки в файл
let logError (message: string) (ex: Exception) =
    lock errorLock (fun () ->
        try
            let logFile = "errors.log"
            let timestamp = DateTime.Now.ToString "yyyy-MM-dd HH:mm:ss"
            let logEntry = sprintf "[%s] %s%s%s" timestamp message Environment.NewLine (ex.ToString())
            System.IO.File.AppendAllText(logFile, logEntry + Environment.NewLine + Environment.NewLine)
        with
        | _ -> ()
    )

// Функция для безопасного получения строки из значения
let getStringValue (value: JsonValue) =
    try
        if value.IsNull then None
        else
            let str = value.AsString()
            if String.IsNullOrWhiteSpace str then None
            else Some str
    with
    | _ -> None

// Функция для преобразования из провайдера в тип
let convertItem (providerItem: Ukr200Provider.Item) : Item = 
    try
        {
            Fullname = providerItem.Fullname
            AltFullname = getStringValue providerItem.AltFullname.JsonValue
            Rank = getStringValue providerItem.Rank.JsonValue
            Conscription = getStringValue providerItem.Conscription.JsonValue
            DaysFromConscription = getStringValue providerItem.DaysFromConscription.JsonValue
            Age = getStringValue providerItem.Age.JsonValue
            Sex = getStringValue providerItem.Sex.JsonValue
            DateOfBirth = getStringValue providerItem.DateOfBirth.JsonValue
            DateOfDeath = getStringValue providerItem.DateOfDeath.JsonValue
            DateOfConscription = getStringValue providerItem.DateOfConscription.JsonValue
            DateOfFuneral = getStringValue providerItem.DateOfFuneral.JsonValue
            IsDiedFromWounds = getStringValue providerItem.IsDiedFromWounds.JsonValue
            IsFromDecrees = getStringValue providerItem.IsFromDecrees.JsonValue
            Sources = getStringValue providerItem.Sources.JsonValue
            DeathAt = getStringValue providerItem.DeathAt.JsonValue
            RegionOfLive = getStringValue providerItem.RegionOfLive.JsonValue
            RegionOfDeath = getStringValue providerItem.RegionOfDeath.JsonValue
            RegionOfFuneral = getStringValue providerItem.RegionOfFuneral.JsonValue
        }
    with
    | ex ->
        printfn "Ошибка конвертации для %s: %s" providerItem.Fullname ex.Message
        {
            Fullname = providerItem.Fullname
            AltFullname = None
            Rank = None
            Conscription = None
            DaysFromConscription = None
            Age = None
            Sex = None
            DateOfBirth = None
            DateOfDeath = None
            DateOfConscription = None
            DateOfFuneral = None
            IsDiedFromWounds = None
            IsFromDecrees = None
            Sources = None
            DeathAt = None
            RegionOfLive = None
            RegionOfDeath = None
            RegionOfFuneral = None
        }

// Вспомогательная функция для парсинга дат
let parseDate (str: string option) =
    match str with
    | Some dateStr ->
        if String.IsNullOrWhiteSpace dateStr then
            None
        else
            match DateTime.TryParseExact(
                dateStr, 
                [| "dd.MM.yyyy"; "yyyy.MM.dd"; "dd-MM-yyyy"; "yyyy-MM-dd"; "MM.yyyy"; |], 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None
            ) with
            | true, dt -> Some dt
            | _ -> None
    | _ -> None        

// Парсинг sources
let parseSources (sourcesStr: string option) =
    match sourcesStr with
    | Some s when not (String.IsNullOrWhiteSpace s) ->
        s.Split(',', StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun url -> 
            try
                System.Net.WebUtility.UrlDecode(url.Trim())
            with
            | _ -> url.Trim()
        )
        |> Array.filter (fun url -> not (String.IsNullOrWhiteSpace url))
        |> Array.toList
    | _ -> []

// Создание базы данных
let createDatabase (connectionString: string) =
    use connection = new SqliteConnection(connectionString)
    connection.Open()
    
    // Включаем поддержку внешних ключей
    let pragmaCmd = connection.CreateCommand()
    pragmaCmd.CommandText <- "PRAGMA foreign_keys = ON;"
    pragmaCmd.ExecuteNonQuery() |> ignore
    
    // Создаем таблицу регионов
    let createRegionsCmd = connection.CreateCommand()
    createRegionsCmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS Regions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE
        )
    """
    createRegionsCmd.ExecuteNonQuery() |> ignore

    // Создаем таблицу призыва
    let createConscriptionsCmd = connection.CreateCommand()
    createConscriptionsCmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS Conscriptions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE
        )
    """
    createConscriptionsCmd.ExecuteNonQuery() |> ignore    
    
    // Создаем таблицу званий
    let createRanksCmd = connection.CreateCommand()
    createRanksCmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS Ranks (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE
        )
    """
    createRanksCmd.ExecuteNonQuery() |> ignore
    
    // Создаем таблицу источников
    let createSourcesCmd = connection.CreateCommand()
    createSourcesCmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS Sources (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            url TEXT NOT NULL UNIQUE
        )
    """
    createSourcesCmd.ExecuteNonQuery() |> ignore
    
    // Создаем основную таблицу
    let createCasualtiesCmd = connection.CreateCommand()
    createCasualtiesCmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS Casualties (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            fullname TEXT NOT NULL,
            alt_fullname TEXT,
            rank_id INTEGER,
            conscription_id INTEGER,
            days_from_conscription INTEGER,
            age INTEGER,
            sex TEXT,
            date_of_birth DATE,
            date_of_death DATE,
            date_of_conscription DATE,
            date_of_funeral DATE,
            is_died_from_wounds BOOLEAN DEFAULT 0,
            is_from_decrees BOOLEAN DEFAULT 0,
            death_at DATE,
            region_of_live_id INTEGER,
            region_of_death_id INTEGER,
            region_of_funeral_id INTEGER,
            FOREIGN KEY (rank_id) REFERENCES Ranks(id),
            FOREIGN KEY (conscription_id) REFERENCES Conscriptions(id),
            FOREIGN KEY (region_of_live_id) REFERENCES Regions(id),
            FOREIGN KEY (region_of_death_id) REFERENCES Regions(id),
            FOREIGN KEY (region_of_funeral_id) REFERENCES Regions(id),
            UNIQUE(fullname, death_at)
        )
    """
    createCasualtiesCmd.ExecuteNonQuery() |> ignore
    
    // Создаем связующую таблицу
    let createCasualtiesSourcesCmd = connection.CreateCommand()
    createCasualtiesSourcesCmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS CasualtiesSources (
            casualty_id INTEGER NOT NULL,
            source_id INTEGER NOT NULL,
            PRIMARY KEY (casualty_id, source_id),
            FOREIGN KEY (casualty_id) REFERENCES Casualties(id) ON DELETE CASCADE,
            FOREIGN KEY (source_id) REFERENCES Sources(id) ON DELETE CASCADE
        )
    """
    createCasualtiesSourcesCmd.ExecuteNonQuery() |> ignore
    
    // Создаем индексы
    let createIndexesCmd = connection.CreateCommand()
    createIndexesCmd.CommandText <- """
        CREATE INDEX IF NOT EXISTS idx_casualties_fullname ON Casualties(fullname);
        CREATE INDEX IF NOT EXISTS idx_casualties_death_at ON Casualties(death_at);
        CREATE INDEX IF NOT EXISTS idx_casualties_rank ON Casualties(rank_id);
        CREATE INDEX IF NOT EXISTS idx_casualties_conscription ON Casualties(conscription_id);
        CREATE INDEX IF NOT EXISTS idx_casualties_region_live ON Casualties(region_of_live_id);
        CREATE INDEX IF NOT EXISTS idx_casualties_region_death ON Casualties(region_of_death_id);
        CREATE INDEX IF NOT EXISTS idx_sources_url ON Sources(url);
    """
    createIndexesCmd.ExecuteNonQuery() |> ignore

// Функция для работы со справочными таблицами
let getOrCreateLookup (connection: SqliteConnection) (transaction: SqliteTransaction) (tableName: string) (columnName: string) (name: string option) =
    match name with
    | Some n when not (String.IsNullOrWhiteSpace n) ->
        let trimmedName = n.Trim().ToLower()
        try
            let selectCmd = connection.CreateCommand()
            selectCmd.Transaction <- transaction
            selectCmd.CommandText <- sprintf "SELECT id FROM %s WHERE %s = @name" tableName columnName
            selectCmd.Parameters.AddWithValue("@name", trimmedName) |> ignore
            let result = selectCmd.ExecuteScalar()
            match result with
            | :? int64 as id -> Some (int id)
            | _ ->
                let insertCmd = connection.CreateCommand()
                insertCmd.CommandText <- sprintf """
                    INSERT OR IGNORE INTO %s (%s) VALUES (@name);
                    SELECT id FROM %s WHERE %s = @name;
                """ tableName columnName tableName columnName
                insertCmd.Parameters.AddWithValue("@name", trimmedName) |> ignore
                let newId = insertCmd.ExecuteScalar()
                match newId with
                | :? int64 as id -> Some (int id)
                | _ -> None
        with
        | ex ->
            raise ex
            None
    | _ -> None

// Сохранение записи в базу данных
let saveItem (connection: SqliteConnection) (item: Item) =
    use transaction = connection.BeginTransaction()
    
    try
        let rankId = getOrCreateLookup connection transaction "Ranks" "name" item.Rank
        let conscriptionId = getOrCreateLookup connection transaction "Conscriptions" "name" item.Conscription
        let regionLiveId = getOrCreateLookup connection transaction "Regions" "name" item.RegionOfLive
        let regionDeathId = getOrCreateLookup connection transaction "Regions" "name" item.RegionOfDeath
        let regionFuneralId = getOrCreateLookup connection transaction "Regions" "name" item.RegionOfFuneral
        
        let isDiedFromWounds = 
            match item.IsDiedFromWounds with
            | Some "1" -> 1
            | _ -> 0
        
        let isFromDecrees =
            match item.IsFromDecrees with
            | Some "1" -> 1
            | _ -> 0        
        
        let insertCmd = connection.CreateCommand()
        insertCmd.Transaction <- transaction
        insertCmd.CommandText <- """
            INSERT OR REPLACE INTO Casualties (
                fullname, alt_fullname, rank_id, conscription_id, days_from_conscription,
                age, sex, date_of_birth, date_of_death, date_of_conscription,
                date_of_funeral, is_died_from_wounds, is_from_decrees,
                death_at, region_of_live_id, region_of_death_id, region_of_funeral_id
            ) VALUES (
                @fullname, @alt_fullname, @rank_id, @conscription_id, @days_from_conscription,
                @age, @sex, @date_of_birth, @date_of_death, @date_of_conscription,
                @date_of_funeral, @is_died_from_wounds, @is_from_decrees,
                @death_at, @region_of_live_id, @region_of_death_id, @region_of_funeral_id
            );
            SELECT last_insert_rowid();
        """
        
        insertCmd.Parameters.AddWithValue("@fullname", item.Fullname) |> ignore
        
        let addParam (cmd: SqliteCommand) (name: string) (value: string option) =
            match value with
            | Some v when not (String.IsNullOrWhiteSpace v) -> 
                cmd.Parameters.AddWithValue(name, v) |> ignore
            | _ -> 
                cmd.Parameters.AddWithValue(name, box DBNull.Value) |> ignore
        
        insertCmd.Parameters.AddWithValue("@alt_fullname", 
            match item.AltFullname with Some str -> box str | None -> box DBNull.Value) |> ignore         
        
        insertCmd.Parameters.AddWithValue("@rank_id", 
            match rankId with Some rid -> box rid | None -> box DBNull.Value) |> ignore

        insertCmd.Parameters.AddWithValue("@conscription_id", 
            match conscriptionId with Some rid -> box rid | None -> box DBNull.Value) |> ignore
            
        insertCmd.Parameters.AddWithValue("@days_from_conscription", 
            match item.DaysFromConscription with Some str -> box (Convert.ToInt32 str) | None -> box DBNull.Value) |> ignore                                   
        
        insertCmd.Parameters.AddWithValue("@age", 
            match item.Age with Some str -> box (Convert.ToInt32 str) | None -> box DBNull.Value) |> ignore       

        insertCmd.Parameters.AddWithValue("@sex", 
            match item.Sex with Some str -> box str | None -> box DBNull.Value) |> ignore             
        
        let addDateParam (cmd: SqliteCommand) (name: string) (dateOpt: DateTime option) =
            match dateOpt with
            | Some dt -> cmd.Parameters.AddWithValue(name, dt) |> ignore
            | None -> cmd.Parameters.AddWithValue(name, box DBNull.Value) |> ignore        
        
        addDateParam insertCmd "@date_of_birth" (parseDate item.DateOfBirth)
        addDateParam insertCmd "@date_of_death" (parseDate item.DateOfDeath)
        addDateParam insertCmd "@date_of_conscription" (parseDate item.DateOfConscription)
        addDateParam insertCmd "@date_of_funeral" (parseDate item.DateOfFuneral)
        addDateParam insertCmd "@death_at" (parseDate item.DeathAt)
        
        insertCmd.Parameters.AddWithValue("@is_died_from_wounds", isDiedFromWounds) |> ignore
        insertCmd.Parameters.AddWithValue("@is_from_decrees", isFromDecrees) |> ignore
                
        insertCmd.Parameters.AddWithValue("@region_of_live_id", 
            match regionLiveId with Some rid -> box rid | None -> box DBNull.Value) |> ignore
        insertCmd.Parameters.AddWithValue("@region_of_death_id", 
            match regionDeathId with Some rid -> box rid | None -> box DBNull.Value) |> ignore
        insertCmd.Parameters.AddWithValue("@region_of_funeral_id", 
            match regionFuneralId with Some rid -> box rid | None -> box DBNull.Value) |> ignore
        
        let casualtyId = insertCmd.ExecuteScalar()
        
        // Если запись уже существовала, получаем её id
        let finalCasualtyId =
            if casualtyId = DBNull.Value || (casualtyId :?> int64) = 0L then
                let selectIdCmd = connection.CreateCommand()
                selectIdCmd.CommandText <- """
                        SELECT id FROM Casualties 
                        WHERE fullname = @fullname 
                        AND (date_of_birth = @date_of_birth OR (date_of_birth IS NULL AND @date_of_birth IS NULL))
                        AND (date_of_death = @date_of_death OR (date_of_death IS NULL AND @date_of_death IS NULL))
                        LIMIT 1
                    """
                selectIdCmd.Parameters.AddWithValue("@fullname", item.Fullname) |> ignore
                addDateParam selectIdCmd "@date_of_birth" (parseDate item.DateOfBirth)
                addDateParam selectIdCmd "@date_of_death" (parseDate item.DateOfDeath)
                let id = selectIdCmd.ExecuteScalar()
                match id with
                | :? int64 as id -> id
                | _ -> failwith "Не удалось получить ID записи"
            else
                casualtyId :?> int64
        
        // Обрабатываем источники
        let sources = parseSources item.Sources
        if not (List.isEmpty sources) then
            for sourceUrl in sources do
                match getOrCreateLookup connection transaction "Sources" "url" (Some sourceUrl) with
                | Some sourceId ->
                    let linkCmd = connection.CreateCommand()
                    linkCmd.Transaction <- transaction
                    linkCmd.CommandText <- """
                        INSERT OR IGNORE INTO CasualtiesSources (casualty_id, source_id)
                        VALUES (@casualty_id, @source_id)
                    """
                    linkCmd.Parameters.AddWithValue("@casualty_id", finalCasualtyId) |> ignore
                    linkCmd.Parameters.AddWithValue("@source_id", sourceId) |> ignore
                    linkCmd.ExecuteNonQuery() |> ignore
                | None -> ()
        
        transaction.Commit()
        1
    with
    | ex ->
        transaction.Rollback()
        let errorMessage = sprintf "Ошибка при вставке записи %s" item.Fullname
        logError errorMessage ex
        match ex.InnerException with
        | null -> ()
        | innerEx ->
            let innerErrorMessage = sprintf "Внутренняя ошибка при вставке записи %s" item.Fullname
            logError innerErrorMessage ex
        0

// Асинхронная загрузка данных с сервиса
let fetchDataAsync (letter: string) (page: int) =
    async {
        let url = sprintf "https://lostarmour.info/panel/next/api/public/ukr200/search?letter=%s&page=%d" 
                    (Uri.EscapeDataString letter) page
        
        try
            let! page = Ukr200Provider.AsyncLoad url 
            let items = page.Items |> Array.map convertItem
            let paginator = {
                OnPage = page.Paginator.OnPage
                PerPage = page.Paginator.PerPage
                Page = page.Paginator.Page
                Pages = page.Paginator.Pages
                Total = page.Paginator.Total
            }
            return Some { Items = items; Paginator = paginator }            
        with
        | ex -> 
            lock Console.Out (fun () ->
                printfn "Ошибка при загрузке для буквы %s страница %d: %s" letter page ex.Message
            )
            return None
    }

// Производитель - загружает данные и помещает в очередь
let producer (letter: string) (maxRetries: int) 
             (queue: BlockingCollection<Item>) (cancellationToken: CancellationToken) =
    async {
        try
            let! firstPageData = fetchDataAsync letter 1
            
            match firstPageData with
            | Some data ->
                let totalPages = data.Paginator.Pages
                let totalItems = data.Paginator.Total
                let items = data.Items
                
                lock Console.Out (fun () ->
                    printfn "Producer [%s]: начал загрузку, найдено %d страниц, %d записей" letter totalPages totalItems
                )
                
                // Добавляем первую страницу в очередь
                for item in items do
                    if not cancellationToken.IsCancellationRequested then
                        queue.Add item
                
                // Загружаем остальные страницы с использованием рекурсии
                let rec loadPages page =
                    async {
                        if page > totalPages || cancellationToken.IsCancellationRequested then
                            return ()
                        else
                            let rec loadWithRetry attempt =
                                async {
                                    let! result = fetchDataAsync letter page
                                    match result with
                                    | Some pageData -> 
                                        for item in pageData.Items do
                                            if not cancellationToken.IsCancellationRequested then
                                                queue.Add(item)
                                        return true
                                    | None when attempt < maxRetries ->
                                        lock Console.Out (fun () ->
                                            printfn "Producer [%s]: повторная попытка %d/%d страница %d" 
                                                letter (attempt + 1) maxRetries page
                                        )
                                        do! Async.Sleep (1000 * (attempt + 1))
                                        return! loadWithRetry (attempt + 1)
                                    | None ->
                                        lock Console.Out (fun () ->
                                            printfn "Producer [%s]: не удалось загрузить страницу %d" letter page
                                        )
                                        return false
                                }
                            
                            let! success = loadWithRetry 1
                            if success then
                                do! loadPages (page + 1)
                    }
                
                do! loadPages 2
                
                lock Console.Out (fun () ->
                    printfn "Producer [%s]: завершил загрузку" letter
                )
            | None ->
                lock Console.Out (fun () ->
                    printfn "Producer [%s]: не удалось загрузить первую страницу" letter
                )
        with
        | ex ->
            logError (sprintf "Producer [%s]: критическая ошибка" letter) ex
    }

// Потребитель - сохраняет данные из очереди в БД
let consumer (connectionString: string) (queue: BlockingCollection<Item>) 
             (cancellationToken: CancellationToken) (consumerId: int) =
    async {
        use connection = new SqliteConnection(connectionString)
        connection.Open()
        
        let mutable savedCount = 0
        let mutable errorCount = 0
        
        lock Console.Out (fun () ->
            printfn "Consumer %d: запущен" consumerId
        )
        
        try
            use enumerator = queue.GetConsumingEnumerable().GetEnumerator()
            
            while not cancellationToken.IsCancellationRequested && enumerator.MoveNext() do
                let item = enumerator.Current
                if item <> Unchecked.defaultof<Item> then
                    try
                        let result = saveItem connection item
                        if result > 0 then
                            savedCount <- savedCount + 1
                        else
                            errorCount <- errorCount + 1
                    with
                    | ex ->
                        logError (sprintf "Consumer %d: ошибка при сохранении" consumerId) ex
                        errorCount <- errorCount + 1
            
            lock Console.Out (fun () ->
                printfn "Consumer %d: завершил работу. Сохранено: %d, Ошибок: %d" 
                    consumerId savedCount errorCount
            )
        with
        | ex ->
            logError (sprintf "Consumer %d: критическая ошибка" consumerId) ex
    }

let backupExistingDatabase (connectionString: string) =
    try
        // Извлекаем имя файла из строки подключения
        let dbFile = 
            let parts = connectionString.Split(';')
            parts 
            |> Array.tryFind (fun p -> p.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            |> Option.map (fun p -> p.Split('=').[1].Trim())
        
        match dbFile with
        | Some file when System.IO.File.Exists file ->
            let fileInfo = System.IO.FileInfo file
            let fileSize = fileInfo.Length
            
            // Если файл пустой или очень маленький (< 1KB), не делаем бэкап
            if fileSize < 1024L then
                printfn "Файл БД пустой или слишком маленький (размер: %d байт). Бэкап не требуется." fileSize
            else
                // Создаем имя для бэкапа с датой и временем
                let timestamp = DateTime.Now.ToString "yyyy-MM-dd_HH-mm-ss"
                let backupFileName = sprintf "%s_%s.db.bak" timestamp (System.IO.Path.GetFileNameWithoutExtension file)
                let backupPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName file, backupFileName)
                
                // Проверяем, существует ли уже бэкап с таким именем
                if System.IO.File.Exists backupPath then
                    printfn "Бэкап с именем %s уже существует. Перезапись..." backupFileName
                    System.IO.File.Delete backupPath
                
                // Копируем файл
                System.IO.File.Copy(file, backupPath)
                printfn "Создан бэкап базы данных: %s" backupFileName
                printfn "   Размер: %.2f MB" (float fileSize / (1024.0 * 1024.0))
            System.IO.File.Delete file
        | Some file ->
            printfn "Файл БД не найден: %s" file
        | None ->
            printfn "Не удалось определить имя файла БД из строки подключения"
    with
    | ex ->
        printfn "Ошибка при создании бэкапа: %s" ex.Message  
        
// Функция для обработки отмены по Ctrl+C
let setupCancellationHandler (cancellationTokenSource: CancellationTokenSource) =
    // Подписываемся на событие Ctrl+C
    Console.CancelKeyPress.Add(fun args ->
        args.Cancel <- true // Отменяем стандартную обработку, чтобы самим завершить
        printfn "\nПолучен сигнал Ctrl+C. Завершение работы..."
        try
            cancellationTokenSource.Cancel()
            printfn "Отмена запрошена. Ожидаем завершения потребителей..."
        with
        | ex -> printfn "Ошибка при отмене: %s" ex.Message
    )        

// Основная функция
let main() =
    let alphabet = [ 
        "А"; "Б"; "В"; "Г"; "Д"; "Е"; "Є"; "Ж"; "З"; "И"; "І"; "Ї"; 
        "Й"; "К"; "Л"; "М"; "Н"; "О"; "П"; "Р"; "С"; "Т"; "У"; "Ф"; 
        "Х"; "Ц"; "Ч"; "Ш"; "Щ"; "Ю"; "Я"
    ] 
    
    let connectionString = "Data Source=casualties.db"
    let maxRetries = 3
    let producerCount = 4 
    let consumerCount = 3
    let queueCapacity = 10000 
    
    backupExistingDatabase connectionString
    
    printfn "\nСоздание базы данных..."
    createDatabase connectionString    
    
    use queue = new BlockingCollection<Item>(queueCapacity)
    use cancellationTokenSource = new CancellationTokenSource()
    let cancellationToken = cancellationTokenSource.Token
    // Настраиваем обработку Ctrl+C
    setupCancellationHandler cancellationTokenSource    
    
    printfn "\nЗапуск Producer-Consumer системы..."
    printfn "Производителей: %d, Потребителей: %d, Вместимость очереди: %d" 
        producerCount consumerCount queueCapacity
    printfn "Для остановки нажмите Ctrl+C"
    
    let startTime = DateTime.Now
    
    // Запускаем потребителей
    let consumerTasks = 
        [1..consumerCount]
        |> List.map (fun id -> 
            consumer connectionString queue cancellationToken id
            |> Async.StartAsTask
            :> Task
        )
    
    // Разбиваем алфавит на части для производителей
    let chunks = 
        alphabet 
        |> List.chunkBySize (max 1 (alphabet.Length / producerCount))
    
    // Запускаем производителей
    let producerTasks = 
        chunks
        |> List.map (fun chunk ->
            async {
                for letter in chunk do
                    do! producer letter maxRetries queue cancellationToken
            }
            |> Async.StartAsTask
            :> Task
        )
    
    // Ждем завершения ВСЕХ производителей
    printfn "\nОжидание завершения всех производителей..."
    Task.WaitAll(producerTasks |> List.toArray) |> ignore
    printfn "Все производители завершили работу"
    
    // Сигнализируем, что добавление завершено
    printfn "Сигнализируем о завершении добавления в очередь..."
    queue.CompleteAdding()
    printfn "Очередь помечена как завершенная"
    
    // Ждем завершения всех потребителей
    printfn "\nОжидание завершения всех потребителей..."
    try
        Task.WaitAll(consumerTasks |> List.toArray) |> ignore
        printfn "Все потребители завершили работу"
    with
    | :? OperationCanceledException ->
        printfn "Потребители остановлены по запросу"
    | ex ->
        printfn "Ошибка при ожидании потребителей: %s" ex.Message
    
    let elapsed = DateTime.Now - startTime
    
    // Проверяем результат
    use connection = new SqliteConnection(connectionString)
    connection.Open()
    
    let checkCmd = connection.CreateCommand()
    checkCmd.CommandText <- """
        SELECT 
            COUNT(*) as total,
            COUNT(DISTINCT fullname) as unique_names
        FROM Casualties
    """
    use reader = checkCmd.ExecuteReader()
    
    printfn "\n==================== РЕЗУЛЬТАТ ===================="
    if reader.Read() then
        printfn "Всего записей в БД: %d" (reader.GetInt32(0))
        printfn "Уникальных имен: %d" (reader.GetInt32(1))
    
    // Проверяем распределение по буквам
    let statsCmd = connection.CreateCommand()
    statsCmd.CommandText <- """
        SELECT 
            SUBSTR(fullname, 1, 1) as first_letter,
            COUNT(*) as count
        FROM Casualties
        GROUP BY first_letter
        ORDER BY first_letter
    """
    use statsReader = statsCmd.ExecuteReader()
    printfn "\nРаспределение по буквам:"
    while statsReader.Read() do
        let letter = statsReader.GetString(0)
        let count = statsReader.GetInt32(1)
        printfn "  %s: %d" letter count
    
    printfn "\nВремя выполнения: %s" (elapsed.ToString @"mm\:ss")
    printfn "=================================================="
    
    connection.Close()

// Запуск
try
    main()
with
| ex -> 
    printfn "\nКРИТИЧЕСКАЯ ОШИБКА: %s" ex.Message
    printfn "Стек вызовов: %s" ex.StackTrace
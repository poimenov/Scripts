-- ~/scite_lexers.lua
-- Кастомные лексеры для SciTE: Razor (.cshtml, .razor) и TypeScript JSX (.tsx, .jsx)
-- Совместимо с Lua 5.1 / 5.3 / 5.4

-- ============================================================================
-- ОБЩИЕ ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
-- ============================================================================

local function style_word(styler, keywords_table, style_if_match, style_default)
    local word = ""
    local pos = styler:Position()
    local doc_len = editor.Length

    while pos < doc_len do
        local code = editor.CharAt[pos]
        if not code or code <= 0 then break end
        local c = string.char(code)
        if not c:match("[%w_$]") then break end
        word = word .. c
        pos = pos + 1
    end

    if keywords_table[word] then
        styler:SetState(style_if_match)
    else
        styler:SetState(style_default)
    end

    for i = 1, #word do
        styler:Forward()
    end
    styler:SetState(style_default)
end

-- ============================================================================
-- 1. ЛЕКСЕР RAZOR (.cshtml, .razor) -> script_razor
-- ============================================================================

local csharp_keywords = {
    ["abstract"]=1, ["as"]=1, ["base"]=1, ["bool"]=1, ["break"]=1, ["byte"]=1,
    ["case"]=1, ["catch"]=1, ["char"]=1, ["checked"]=1, ["class"]=1, ["const"]=1,
    ["continue"]=1, ["decimal"]=1, ["default"]=1, ["delegate"]=1, ["do"]=1,
    ["double"]=1, ["else"]=1, ["enum"]=1, ["event"]=1, ["explicit"]=1, ["extern"]=1,
    ["false"]=1, ["finally"]=1, ["fixed"]=1, ["float"]=1, ["for"]=1, ["foreach"]=1,
    ["goto"]=1, ["if"]=1, ["implicit"]=1, ["in"]=1, ["int"]=1, ["interface"]=1,
    ["internal"]=1, ["is"]=1, ["lock"]=1, ["long"]=1, ["namespace"]=1, ["new"]=1,
    ["null"]=1, ["object"]=1, ["operator"]=1, ["out"]=1, ["override"]=1, ["params"]=1,
    ["private"]=1, ["protected"]=1, ["public"]=1, ["readonly"]=1, ["ref"]=1,
    ["return"]=1, ["sbyte"]=1, ["sealed"]=1, ["short"]=1, ["sizeof"]=1,
    ["stackalloc"]=1, ["static"]=1, ["string"]=1, ["struct"]=1, ["switch"]=1,
    ["this"]=1, ["throw"]=1, ["true"]=1, ["try"]=1, ["typeof"]=1, ["uint"]=1,
    ["ulong"]=1, ["unchecked"]=1, ["unsafe"]=1, ["ushort"]=1, ["using"]=1,
    ["virtual"]=1, ["void"]=1, ["volatile"]=1, ["while"]=1, ["var"]=1, ["async"]=1,
    ["await"]=1, ["record"]=1, ["init"]=1, ["required"]=1, ["get"]=1, ["set"]=1,
    ["value"]=1, ["partial"]=1, ["yield"]=1, ["Task"]=1, ["List"]=1, ["IEnumerable"]=1
}

local razor_block_starters = {
    ["@code"]=1, ["@functions"]=1, ["@if"]=1, ["@for"]=1, ["@foreach"]=1,
    ["@while"]=1, ["@switch"]=1, ["@try"]=1, ["@using"]=1, ["@lock"]=1
}

local function style_razor(styler)
    local S_DEFAULT = 0
    local S_TAG = 1
    local S_ATTR = 2
    local S_STRING = 3
    local S_COMMENT = 4
    local S_RAZOR = 5
    local S_CSHARP_KW = 6
    local S_NUMBER = 7

    styler:StartStyling(styler.startPos, styler.lengthDoc, styler.initStyle)

    local current_line = editor:LineFromPosition(styler.startPos)
    local brace_depth = 0
    if current_line > 0 then
        brace_depth = editor.LineState[current_line - 1] or 0
        if brace_depth < 0 then brace_depth = 0 end
    end

    local in_html_tag = false
    local expect_razor_brace = false
    local in_line_comment = false

    while styler:More() do
        local ch = styler:Current()
        local state = styler:State()
        local stepped = false

        if ch == "\n" then
            in_line_comment = false
            editor.LineState[current_line] = brace_depth
            current_line = current_line + 1
            if state == S_COMMENT then
                styler:ForwardSetState(S_DEFAULT)
                stepped = true
            end
        end

        if not stepped then
            if state ~= S_STRING and state ~= S_COMMENT then
                if ch == "@" and styler:Next() == "*" then
                    styler:SetState(S_COMMENT)
                    styler:Forward()
                    styler:Forward()
                    stepped = true
                elseif styler:Match("<!--") then
                    styler:SetState(S_COMMENT)
                    styler:Forward()
                    stepped = true
                elseif brace_depth > 0 and ch == "/" and styler:Next() == "/" then
                    in_line_comment = true
                    styler:SetState(S_COMMENT)
                    styler:Forward()
                    stepped = true
                elseif brace_depth > 0 and ch == "/" and styler:Next() == "*" then
                    styler:SetState(S_COMMENT)
                    styler:Forward()
                    stepped = true
                end
            elseif state == S_COMMENT then
                if in_line_comment and (ch == "\r" or ch == "\n") then
                    in_line_comment = false
                    styler:SetState(S_DEFAULT)
                elseif not in_line_comment then
                    if ch == "*" and styler:Next() == "@" then
                        styler:Forward()
                        styler:ForwardSetState(S_DEFAULT)
                        stepped = true
                    elseif ch == "*" and styler:Next() == "/" then
                        styler:Forward()
                        styler:ForwardSetState(S_DEFAULT)
                        stepped = true
                    elseif styler:Match("-->") then
                        styler:Forward()
                        styler:Forward()
                        styler:ForwardSetState(S_DEFAULT)
                        stepped = true
                    end
                end
                if not stepped then
                    styler:Forward()
                    stepped = true
                end
            end
        end

        if not stepped then
            if state ~= S_STRING and (ch == '"' or ch == "'") then
                styler:SetState(S_STRING)
                styler:Forward()
                stepped = true
            elseif state == S_STRING then
                if ch == "\\" then
                    styler:Forward()
                elseif ch == '"' or ch == "'" then
                    styler:ForwardSetState(in_html_tag and S_TAG or S_DEFAULT)
                    stepped = true
                end
                if not stepped then
                    styler:Forward()
                    stepped = true
                end
            end
        end

        if not stepped and ch == "@" and styler:Next() ~= "@" and not in_html_tag then
            styler:SetState(S_RAZOR)
            local word = "@"
            styler:Forward()
            while styler:More() and styler:Current():match("[%w_]") do
                word = word .. styler:Current()
                styler:Forward()
            end

            if word == "@" and (styler:Current() == "{" or styler:Current() == "(") then
                if styler:Current() == "{" then
                    brace_depth = brace_depth + 1
                end
                styler:ForwardSetState(S_DEFAULT)
                stepped = true
            else
                if razor_block_starters[word] then
                    expect_razor_brace = true
                end
                styler:SetState(S_DEFAULT)
                stepped = true
            end
        end

        if not stepped and (brace_depth > 0 or expect_razor_brace) then
            if ch == "{" then
                brace_depth = brace_depth + 1
                expect_razor_brace = false
                styler:SetState(S_RAZOR)
                styler:ForwardSetState(S_DEFAULT)
                stepped = true
            elseif ch == "}" then
                brace_depth = brace_depth - 1
                if brace_depth < 0 then brace_depth = 0 end
                styler:SetState(S_RAZOR)
                styler:ForwardSetState(S_DEFAULT)
                stepped = true
            elseif ch == "<" and styler:Next():match("[%a/!]") then
                in_html_tag = true
                styler:SetState(S_TAG)
                styler:Forward()
                stepped = true
            elseif in_html_tag then
                if ch == ">" then
                    in_html_tag = false
                    styler:SetState(S_TAG)
                    styler:ForwardSetState(S_DEFAULT)
                elseif ch:match("%s") then
                    styler:SetState(S_ATTR)
                elseif ch == "=" then
                    styler:SetState(S_DEFAULT)
                end
                if not stepped then
                    styler:Forward()
                    stepped = true
                end
            elseif ch:match("[%a_]") then
                style_word(styler, csharp_keywords, S_CSHARP_KW, S_DEFAULT)
                stepped = true
            elseif ch:match("%d") then
                styler:SetState(S_NUMBER)
                styler:Forward()
                stepped = true
            else
                styler:SetState(S_DEFAULT)
                styler:Forward()
                stepped = true
            end
        end

        if not stepped then
            if ch == "<" and styler:Next():match("[%a/!]") then
                in_html_tag = true
                styler:SetState(S_TAG)
            elseif in_html_tag then
                if ch == ">" then
                    in_html_tag = false
                    styler:SetState(S_TAG)
                    styler:ForwardSetState(S_DEFAULT)
                    stepped = true
                elseif ch:match("%s") then
                    styler:SetState(S_ATTR)
                elseif ch == "=" then
                    styler:SetState(S_DEFAULT)
                end
            else
                styler:SetState(S_DEFAULT)
            end

            if not stepped then
                styler:Forward()
            end
        end
    end

    editor.LineState[current_line] = brace_depth
    styler:EndStyling()
end

-- ============================================================================
-- 2. ЛЕКСЕР TSX / JSX (.tsx, .jsx) -> script_tsx
-- ============================================================================
local ts_keywords = {
    ["abstract"]=1, ["any"]=1, ["as"]=1, ["asserts"]=1, ["async"]=1, ["await"]=1,
    ["bigint"]=1, ["boolean"]=1, ["break"]=1, ["case"]=1, ["catch"]=1, ["class"]=1,
    ["const"]=1, ["continue"]=1, ["debugger"]=1, ["declare"]=1, ["default"]=1,
    ["delete"]=1, ["do"]=1, ["else"]=1, ["enum"]=1, ["export"]=1, ["extends"]=1,
    ["false"]=1, ["finally"]=1, ["for"]=1, ["from"]=1, ["function"]=1, ["get"]=1,
    ["if"]=1, ["implements"]=1, ["import"]=1, ["in"]=1, ["infer"]=1, ["instanceof"]=1,
    ["interface"]=1, ["is"]=1, ["keyof"]=1, ["let"]=1, ["module"]=1, ["namespace"]=1,
    ["never"]=1, ["new"]=1, ["null"]=1, ["number"]=1, ["object"]=1, ["of"]=1,
    ["override"]=1, ["package"]=1, ["private"]=1, ["protected"]=1, ["public"]=1,
    ["readonly"]=1, ["require"]=1, ["return"]=1, ["satisfies"]=1, ["set"]=1,
    ["static"]=1, ["string"]=1, ["super"]=1, ["switch"]=1, ["symbol"]=1, ["this"]=1,
    ["throw"]=1, ["true"]=1, ["try"]=1, ["type"]=1, ["typeof"]=1, ["undefined"]=1,
    ["unique"]=1, ["unknown"]=1, ["var"]=1, ["void"]=1, ["while"]=1, ["with"]=1,
    ["yield"]=1,
    ["React"]=1, ["FC"]=1, ["ReactNode"]=1, ["JSX"]=1, ["useState"]=1,
    ["useEffect"]=1, ["useMemo"]=1, ["useCallback"]=1, ["useRef"]=1, ["useContext"]=1,
    ["Promise"]=1, ["Record"]=1, ["Partial"]=1, ["Pick"]=1, ["Omit"]=1
}

local function style_tsx(styler)
    local S_DEFAULT = 0
    local S_TAG = 1
    local S_ATTR = 2
    local S_STRING = 3
    local S_COMMENT = 4
    local S_TS_KW = 5
    local S_NUMBER = 6
    local S_BRACE = 7

    styler:StartStyling(styler.startPos, styler.lengthDoc, styler.initStyle)

    local in_jsx_tag = false
    local string_quote = ""
    local in_line_comment = false
    local in_block_comment = false

    local current_line = editor:LineFromPosition(styler.startPos)
    if current_line > 0 and editor.LineState[current_line - 1] == 1 then
        in_block_comment = true
        styler:SetState(S_COMMENT)
    end

    while styler:More() do
        local ch = styler:Current()
        local stepped = false

        if ch == "\n" then
            if in_line_comment then
                in_line_comment = false
                styler:ForwardSetState(S_DEFAULT)
                stepped = true
            end
            editor.LineState[current_line] = in_block_comment and 1 or 0
            current_line = current_line + 1
        end

        if not stepped then
            if in_line_comment then
                styler:Forward()
                stepped = true
            elseif in_block_comment then
                if ch == "*" and styler:Next() == "/" then
                    in_block_comment = false
                    styler:Forward()
                    styler:ForwardSetState(S_DEFAULT)
                else
                    styler:Forward()
                end
                stepped = true
            elseif string_quote == "" then
                if ch == "/" and styler:Next() == "/" then
                    in_line_comment = true
                    styler:SetState(S_COMMENT)
                    styler:Forward()
                    stepped = true
                elseif ch == "/" and styler:Next() == "*" then
                    in_block_comment = true
                    styler:SetState(S_COMMENT)
                    styler:Forward()
                    stepped = true
                end
            end
        end

        if not stepped then
            if string_quote == "" and (ch == '"' or ch == "'" or ch == "`") then
                string_quote = ch
                styler:SetState(S_STRING)
                styler:Forward()
                stepped = true
            elseif string_quote ~= "" then
                if ch == string.char(92) then
                    styler:Forward()
                elseif ch == string_quote then
                    string_quote = ""
                    styler:ForwardSetState(in_jsx_tag and S_TAG or S_DEFAULT)
                    stepped = true
                end
                if not stepped then
                    styler:Forward()
                    stepped = true
                end
            end
        end

        if not stepped then
            if not in_jsx_tag and ch == "<" then
                local nxt = styler:Next()
                if nxt:match("[%a/$_>]") then
                    in_jsx_tag = true
                    styler:SetState(S_TAG)
                    styler:Forward()
                    stepped = true
                end
            elseif in_jsx_tag then
                if ch == ">" then
                    in_jsx_tag = false
                    styler:SetState(S_TAG)
                    styler:ForwardSetState(S_DEFAULT)
                    stepped = true
                elseif ch == "{" then
                    in_jsx_tag = false
                    styler:SetState(S_BRACE)
                    styler:ForwardSetState(S_DEFAULT)
                    stepped = true
                elseif ch:match("%s") then
                    styler:SetState(S_ATTR)
                    styler:Forward()
                    stepped = true
                elseif ch == "=" or ch == "/" then
                    styler:SetState(S_TAG)
                    styler:Forward()
                    stepped = true
                else
                    styler:Forward()
                    stepped = true
                end
            end
        end

        if not stepped then
            if ch == "{" or ch == "}" then
                styler:SetState(S_BRACE)
                styler:ForwardSetState(S_DEFAULT)
                stepped = true
            elseif ch:match("[%a_$]") then
                style_word(styler, ts_keywords, S_TS_KW, S_DEFAULT)
                stepped = true
            elseif ch:match("%d") then
                styler:SetState(S_NUMBER)
                styler:Forward()
                stepped = true
            else
                styler:SetState(S_DEFAULT)
                styler:Forward()
                stepped = true
            end
        end
    end

    editor.LineState[current_line] = in_block_comment and 1 or 0
    styler:EndStyling()
end

function OnStyle(styler)
    if styler.language == "script_razor" then
        style_razor(styler)
    elseif styler.language == "script_tsx" then
        style_tsx(styler)
    end
end

-- ============================================================================
-- ТОЧКА ВХОДА ДЛЯ ВСЕХ СКРИПТОВЫХ ЛЕКСЕРОВ SCITE
-- ============================================================================
function OnStyle(styler)
    if styler.language == "script_razor" then
        style_razor(styler)
    elseif styler.language == "script_tsx" then
        style_tsx(styler)
    end
end
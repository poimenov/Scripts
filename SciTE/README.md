# Настройка SciTE для Razor (`.cshtml`, `.razor`), TSX/JSX (`.tsx`, `.jsx`) и UTF-8 в Linux

Памятка по конфигурации редактора **SciTE** (Lubuntu / Ubuntu) для подсветки смешанного синтаксиса ASP.NET Core / Blazor Razor и TypeScript React (TSX/JSX), работы с кодировкой UTF-8 и сборки проектов.

---

## 1. Структура и расположение файлов

В Linux пользовательские настройки и скрипты хранятся в домашней директории (`~/` или `/home/<username>`):

* **`~/.SciTEUser.properties`** — пользовательский файл конфигурации SciTE (переопределяет системный `/usr/share/scite/SciTEGlobal.properties`). Открывается через меню редактора: **Options → Open User Options File**.
* **`~/scite_lexers.lua`** — объединенный кастомный лексер на Lua (совместим с Lua 5.1 / 5.3 / 5.4), содержащий два обработчика: `script_razor` и `script_tsx`.

---

## 2. Настройки в `~/.SciTEUser.properties`

### Кодировка UTF-8 по умолчанию
Принудительное открытие и создание файлов в UTF-8 без BOM (стандарт для Linux, .NET и Node.js):

```properties
code.page=65001
character.set=204
```

### Подключение скрипта лексеров
В SciTE параметр `ext.lua.startup.script` принимает один файл, поэтому оба лексера объединены в `scite_lexers.lua` и маршрутизируются через `styler.language`:

```properties
ext.lua.startup.script=\$(SciteUserHome)/scite_lexers.lua
```

### Подсветка и команды для Razor (`.cshtml`, `.razor`)

```properties
file.patterns.razor=*.cshtml;*.razor
lexer.\$(file.patterns.razor)=script_razor

style.script_razor.0=fore:#000000                    # Обычный текст
style.script_razor.1=fore:#000080,bold               # HTML-теги
style.script_razor.2=fore:#008080                    # HTML-атрибуты
style.script_razor.3=fore:#A31515                    # Строки ("..." и '...')
style.script_razor.4=fore:#008000,italics            # Комментарии (@* *@, <!-- -->, //, /* */)
style.script_razor.5=fore:#800080,back:#F3E8FF,bold  # Директивы Razor (@page, @code, @if и @)
style.script_razor.6=fore:#000080,bold               # Ключевые слова C# (Navy)
style.script_razor.7=fore:#FF6600                    # Числа

# Сборка и запуск .NET по F7 / F5 (опционально)
command.build.\$(file.patterns.razor)=dotnet build
command.go.\$(file.patterns.razor)=dotnet run
command.go.subsystem.\$(file.patterns.razor)=0
```

### Подсветка и команды для TSX / JSX (`.tsx`, `.jsx`)

```properties
file.patterns.tsx=*.tsx;*.jsx
lexer.\$(file.patterns.tsx)=script_tsx

style.script_tsx.0=fore:#000000                    # Обычный текст
style.script_tsx.1=fore:#000080,bold               # JSX-теги
style.script_tsx.2=fore:#008080                    # JSX-атрибуты
style.script_tsx.3=fore:#A31515                    # Строки ("...", '...', `...`)
style.script_tsx.4=fore:#008000,italics            # Комментарии (// и /* */)
style.script_tsx.5=fore:#000080,bold               # Ключевые слова TypeScript и хуки React (Navy)
style.script_tsx.6=fore:#FF6600                    # Числа
style.script_tsx.7=fore:#800080,bold               # Фигурные скобки { }

# Проверка типов TypeScript по F7 (опционально)
command.build.\$(file.patterns.tsx)=npx tsc --noEmit
```

---

## 3. Устройство и кастомизация `scite_lexers.lua`

* **`csharp_keywords` и `ts_keywords`** — словари ключевых слов, типов и хуков для C# и TypeScript/React. Сюда можно добавлять любые пользовательские типы или классы для подсветки цветом ключевых слов.
* **`razor_block_starters`** — список блочных директив Razor (`@code`, `@functions`, `@if`, `@foreach` и др.), переводящих лексер в режим C#.
* **`editor.LineState`** — массив состояний строк Scintilla:
  * в `style_razor` хранит глубину вложенности скобок `{ ... }`;
  * в `style_tsx` отслеживает многострочные комментарии `/* ... */`.
* **`OnStyle(styler)`** — общая точка входа, проверяющая `styler.language` (`script_razor` или `script_tsx`) и вызывающая нужный обработчик.

---

## 4. Применение изменений

* Правки в **`.SciTEUser.properties`** применяются **сразу после сохранения** (`Ctrl+S`).
* Правки в **`scite_lexers.lua`** вступают в силу после **перезапуска SciTE**.

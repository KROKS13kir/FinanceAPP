# FinanceAPP — руководство по установке и запуску

Проект состоит из трёх частей:
- **Консольное приложение (.NET 8)** — папка `Finance`
- **Бэкенд API (.NET 8 Minimal API)** — папка `WEBFinanceApi`
- **Клиент (Vue 3 + Vite)** — папка `personal-finance-ui`
- **Юнит‑тесты .NET** — папка `tests`

## 1) Предварительные требования

- **.NET SDK 8.0+** — установить: https://dotnet.microsoft.com/download  
  Проверка версии: `dotnet --version`
- **Node.js 18+ (LTS)** — установить: https://nodejs.org/  
  Проверка версии: `node -v && npm -v`
- **Git** — https://git-scm.com/

---

## 2) Установка зависимостей

### Вариант A — одной командой для всех .NET‑проектов
В корне решения:
```bash
dotnet restore
```

### Вариант B — по папкам
```bash
# Бэкенд
cd WEBFinanceApi && dotnet restore && cd ..

# Консольное приложение
cd Finance && dotnet restore && cd ..

# Тесты
cd tests/Finance.Tests && dotnet restore && cd ../..
```

### Клиент (Vue + Vite)
```bash
cd personal-finance-ui
npm ci        # предпочтительно при наличии package-lock.json (иначе: npm install)
# укажем URL API (порт по умолчанию 5001):
echo "VITE_API_BASE=http://localhost:5001" > .env
cd ..
```

---

## 3) Запуск

### 3.1 Консольное приложение (.NET)
Позволяет управлять кошельками и транзакциями через меню в терминале.
```bash
cd Finance
dotnet run
```
Функции консольного приложения (из меню):
- выбрать источник данных (генерация примера / загрузка из JSON‑файла / ручной ввод);
- показать список кошельков и транзакций;
- добавить транзакцию (с проверкой «нельзя потратить больше текущего баланса»);
- сохранить текущие данные в JSON;
- построить отчёт за месяц: сгруппировать транзакции по типу (Income/Expense), внутри отсортировать по дате (↑), вывести топ‑3 расходов по каждому кошельку.


### 3.2 Бэкенд API (.NET Minimal API)
HTTP API для хранения данных и отчётов.
```bash
cd WEBFinanceApi
dotnet run
```
По умолчанию API поднимется на `http://localhost:5001` (см. `WEBFinanceApi/Properties/launchSettings.json`).  
Swagger доступен в Dev‑режиме: `http://localhost:5001/swagger`.

Ключевые эндпоинты:
- `GET  /api/wallets` — получить все кошельки;
- `POST /api/wallets` — перезаписать все кошельки (импорт JSON);
- `POST /api/wallets/create` — создать кошелёк;
- `POST /api/wallets/{id}/transactions` — добавить транзакцию (с проверкой баланса);
- `POST /api/sample` — сгенерировать демо‑данные;
- `GET  /api/report?year=YYYY&month=MM[&currency=RUB]` — отчёт с группировкой по валюте и типу.

Данные сохраняются в файл `finance.json` рядом с исполняемым файлом API.

### 3.3 Клиент (Vue 3 + Vite)
Веб‑интерфейс для работы с данными и отчётами.
```bash
cd personal-finance-ui
npm run dev
```
Откройте **http://localhost:5173**. Клиент читает адрес API из `VITE_API_BASE` (файл `.env`).


---

## 4) Тесты (.NET)

Запуск всех тестов решения:
```bash
dotnet test -v minimal
```

---

## 5) Полезные ссылки

- .NET SDK: https://dotnet.microsoft.com/download
- Node.js (LTS): https://nodejs.org/
- Vite: https://vitejs.dev/
- Vue 3: https://vuejs.org/
- xUnit: https://xunit.net/
- Swagger / OpenAPI: https://swagger.io/

# POE-PART3
**Cybersecurity Awareness Chatbot | WPF C# | .NET 8**

---

## Features

| Task | Description |
|------|-------------|
| Task 1 | Task Assistant — add, view, complete, and delete cybersecurity tasks stored in MySQL |
| Task 2 | Quiz — 12 cybersecurity questions (multiple choice + true/false) with scoring |
| Task 3 | NLP Chat — natural language input using keyword detection and regex |
| Task 4 | Activity Log — every bot action is recorded in MySQL and shown in a DataGrid |

---

## Setup Instructions

### Step 1 — MySQL Database
1. Open **MySQL Workbench**
2. Run the file `database_setup.sql` (included in the root folder)
3. This creates the database `GuardCyberBotDB` with two tables:
   - `Tasks` — stores cybersecurity tasks and reminders
   - `ActivityLog` — stores every action the chatbot takes

### Step 2 — Update the Connection String
Open `MainWindow.xaml.cs` and change line 22 to match your MySQL password:
```
pwd=YourPasswordHere;
```

### Step 3 — Install NuGet Package
In Visual Studio, right-click the project → **Manage NuGet Packages** → search for and install:
```
MySql.Data  (version 8.3.0)
```
Or via terminal:
```
dotnet add package MySql.Data
```

### Step 4 — Run
Press **F5** in Visual Studio 2022. The project targets **.NET 8 Windows**.

---

## How to Use Each Tab

### Task Assistant (Tab 1)
- Type a task title in the text box
- Optionally pick a reminder date using the date picker
- Click **Add Task** — the task is saved to MySQL and appears in the grid
- Select a task in the grid, then click **Mark Complete** or **Delete Task**
- Click **Refresh** to reload the list from the database

### Quiz (Tab 2)
- Click **Start Quiz** to begin
- 12 questions load one at a time — mix of multiple choice and true/false
- Click one of the A / B / C / D buttons to answer
- Feedback and the correct answer are shown immediately
- The next question loads automatically after 2 seconds
- Final score and feedback are shown when all 12 questions are done
- Click **Restart Quiz** to play again

### NLP Chat (Tab 3)
Type naturally — the bot detects your intent using keyword matching. Examples:

| What you type | What happens |
|---|---|
| `add task to enable 2FA` | Task created in MySQL |
| `remind me to update my password in 3 days` | Task created with a reminder date |
| `show my tasks` | Task list refreshed |
| `complete task 2` | Task #2 marked as done |
| `delete task 3` | Task #3 removed |
| `start quiz` | Prompted to switch to Quiz tab |
| `show activity log` | Log refreshed |

### Activity Log (Tab 4)
- Automatically updated after every action
- Shows the last 10 actions with timestamps, pulled from MySQL
- Click **Refresh Log** to manually reload

---

## File Structure

```
GuardCyberBotPart3/
├── MainWindow.xaml          — GUI layout (4 tabs)
├── MainWindow.xaml.cs       — All logic: tasks, quiz, NLP, activity log
├── CyberTask.cs             — Model for the Tasks table
├── ActivityLogEntry.cs      — Model for the ActivityLog table
├── QuizQuestion.cs          — Model for one quiz question
├── GuardCyberBotPart3.csproj
database_setup.sql           — Run this first in MySQL Workbench
README.md
```

---

## GitHub Requirements
- Minimum **6 commits** with meaningful messages, e.g.:
  1. `Initial WPF project setup`
  2. `Add MySQL database setup and CyberTask model`
  3. `Implement Task Assistant with CRUD operations`
  4. `Add 12-question cybersecurity quiz`
  5. `Implement NLP chat with keyword intent detection`
  6. `Add Activity Log feature with MySQL logging`

- Minimum **3 releases** tagged as: `v1.0`, `v2.0`, `v3.0`

# 🍲 RecipeApp

A cross-platform mobile application built using **.NET MAUI**, designed to help users manage, browse, and discover recipes. This app is developed with performance, simplicity, and user experience in mind.

---

## 📱 Features

- 📖 Browse a curated list of recipes
- 🔍 Search by name, ingredients, or category
- 📝 Add and manage personal recipes
- 🌗 Light/Dark theme support
- 🌍 Cross-platform (Android, Windows, macOS, iOS*)
- 📦 Built with .NET MAUI and C#
- 📲 Android APK available for download

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [.NET MAUI Workloads](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation)
- Visual Studio 2022+ (with Mobile Development workload)

### Clone the Repository

```bash
git clone https://github.com/yourusername/RecipeApp.git
cd RecipeApp

To build and run the project on Android:
dotnet workload install maui-android
dotnet restore
dotnet build -f net9.0-android
dotnet run -f net9.0-android

🧪 Continuous Integration (CI)
This project includes a CI/CD GitHub Actions workflow that:

Restores dependencies

Installs MAUI Android workload

Builds the Android .apk

Publishes it as an artifact for download

📦 The latest .apk is available under the Actions tab after each push to main.

📤 Download APK
You can download the latest Android APK by:

Visiting the Actions tab

Selecting the latest successful build on main

Scrolling to the Artifacts section and downloading RecipeApp-APK

Coming Soon: Automatic GitHub Release with direct public APK download

🧱 Project Structure
RecipeApp/
├── Platforms/           # Platform-specific startup code
├── Pages/               # UI pages and views
├── Models/              # Data models
├── ViewModels/          # MVVM logic
├── Resources/           # Images, styles, fonts
├── RecipeApp.csproj     # MAUI project file
|-- Helpers              # Helper files
└── README.md

📄 Technologies Used
.NET MAUI

C#

GitHub Actions (CI/CD)

MVVM Architecture

XAML UI

📈 Contributing
Contributions are welcome! To contribute:

Fork the repository

Create a new branch (git checkout -b feature-name)

Commit your changes

Push to your fork and submit a Pull Request

📜 License
This project is licensed under the MIT License.

👤 Author
Your Name
GitHub • LinkedIn


---

### ✅ What You Should Do

1. Save this as a file named `README.md` in your project root.
2. Replace:
   - `yourusername` → your actual GitHub username
   - `yourname` → your real name or alias
   - Add any real links for the License, GitHub Releases, etc.

3. Then run:

```bash
git add README.md
git commit -m "Add professional README"
git push

# 🍲 RecipeApp

![CI](https://github.com/PTA-Avenger/RecipeApp/actions/workflows/build-apk.yml/badge.svg)
[![Download APK](https://img.shields.io/badge/Download-APK-blue.svg)](https://github.com/PTA-Avenger/RecipeApp/releases/latest/download/RecipeApp.apk)


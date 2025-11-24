# Search Engine with B+ Tree Inverted Index

A professional search engine implementation using .NET 10 MVC with B+ Tree data structure for inverted indexing.

## Installation & Setup

### 1. Install .NET 10 SDK

**Windows:**
```bash
winget install Microsoft.DotNet.SDK.Preview
```

**macOS:**
```bash
brew install --cask dotnet-sdk-preview
```

**Linux (Ubuntu):**
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
```

Verify installation:
```bash
dotnet --version
```

### 2. Clone and Setup Project

```bash
git clone https://github.com/arshiashirzad/Search-Engine.git
cd "Search Engine/SearchEngine"
dotnet restore
```

### 3. Install Required Packages

The itext7 package should already be installed. If needed, run:
```bash
dotnet add package itext7 --version 8.0.5
```

### 4. Run the Application

**Important:** Make sure you're in the `SearchEngine` subdirectory:
```bash
cd SearchEngine  # if you're in the root "Search Engine" folder
dotnet run
```

Or run directly from root:
```bash
dotnet run --project SearchEngine/SearchEngine.csproj
```

The application will start at `http://localhost:5004`

## Features

- **B+ Tree Data Structure**: Efficient indexing using custom B+ tree implementation
- **PDF Support**: Extract and index text from PDF files using iText7
- **Inverted Index**: Fast document retrieval with term-to-document mapping
- **Intelligent Tokenization**: Smart filtering of numbers, stop words, and garbage terms
- **Phrase Search**: Support for multi-term phrase queries
- **Document Management**: Upload .txt, .md, .pdf files with CRUD operations
- **Clean Architecture**: Follows SOLID principles with proper separation of concerns

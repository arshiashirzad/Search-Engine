# Search Engine with B+ Tree Inverted Index

A professional search engine implementation using .NET 10 MVC with B+ Tree data structure for inverted indexing.

## Features

- **B+ Tree Data Structure**: Efficient indexing using custom B+ tree implementation
- **Inverted Index**: Fast document retrieval with term-to-document mapping
- **Tokenization**: Advanced text processing with normalization and stop-word removal
- **K-gram Support**: Character-level indexing capability
- **Phrase Search**: Support for multi-term phrase queries
- **Document Management**: Full CRUD operations for documents
- **Clean Architecture**: Follows SOLID principles with proper separation of concerns
- **Repository Pattern**: Abstraction layer for data access
- **Dependency Injection**: Loosely coupled components

## Architecture

### Data Structures
- `BPlusTree<TKey, TValue>`: Generic B+ tree with insert, search, and split operations
- `BPlusTreeNode<TKey, TValue>`: Abstract base class for nodes
- `BPlusTreeLeafNode<TKey, TValue>`: Leaf nodes storing actual values
- `BPlusTreeInternalNode<TKey, TValue>`: Internal nodes for navigation

### Services
- `ITokenizer / Tokenizer`: Text tokenization with normalization and K-gram generation
- `IInvertedIndex / InvertedIndex`: B+ tree-based inverted index with phrase search
- `ISearchEngineService / SearchEngineService`: Main search engine with indexing and search
- `IDocumentRepository / DocumentRepository`: Document storage and retrieval

### Models
- `Document`: Entity with Id, Title, Content, DateAdded, IsIndexed
- `SearchResult`: Search result with document and relevance score

## Usage

### Run the Application
```bash
cd SearchEngine
dotnet run
```

The application will start at `http://localhost:5004`

### Features Available

1. **Search**: Search for terms or phrases across indexed documents
2. **Add Documents**: Add new documents to the system
3. **Index Documents**: Index individual or all documents
4. **Manage Documents**: View, edit, and delete documents
5. **Clear Index**: Reset the search index

## Project Structure

```
SearchEngine/
├── Controllers/
│   ├── DocumentController.cs
│   └── SearchController.cs
├── DataStructures/
│   ├── BPlusTree.cs
│   ├── BPlusTreeNode.cs
│   ├── BPlusTreeInternalNode.cs
│   └── BPlusTreeLeafNode.cs
├── Interfaces/
│   ├── IDocumentRepository.cs
│   ├── IInvertedIndex.cs
│   ├── ISearchEngineService.cs
│   └── ITokenizer.cs
├── Models/
│   ├── Document.cs
│   └── SearchResult.cs
├── Repositories/
│   └── DocumentRepository.cs
├── Services/
│   ├── InvertedIndex.cs
│   ├── SearchEngineService.cs
│   └── Tokenizer.cs
└── Views/
    ├── Document/
    │   ├── Create.cshtml
    │   ├── Details.cshtml
    │   └── Index.cshtml
    └── Search/
        └── Index.cshtml
```

## Technical Details

- **Framework**: .NET 10
- **Architecture**: MVC
- **Data Structure**: B+ Tree (Order 4)
- **Indexing**: Inverted Index with position tracking
- **Tokenization**: Lowercase normalization, punctuation removal, stop-word filtering
- **Search Types**: Single term, multi-term (OR), and phrase search

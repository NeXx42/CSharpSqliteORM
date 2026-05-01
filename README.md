A very simple Sqlite ORM for C#. A projet i made to handle my small projects that require a barebones interface with a sqlite database.

## Examples of syntax 

**Tables**
<img width="1110" height="426" alt="image" src="https://github.com/user-attachments/assets/99c6ce15-4099-44d4-a21a-8ebd16c7e358" />

**Migrations**
<img width="772" height="348" alt="image" src="https://github.com/user-attachments/assets/683bc7a3-8681-42f1-b335-e19cb16e2eff" />
>[!WARNING]
>Migrations are only for amending an existing database, they are skipped on new databases.

**Data modifcation**
<img width="954" height="214" alt="image" src="https://github.com/user-attachments/assets/f49739ae-74eb-4be4-b49b-58efb23e5ec5" />
<img width="899" height="78" alt="image" src="https://github.com/user-attachments/assets/cb3a5c6e-2d34-424c-a0cd-55f18670d8df" />
<img width="984" height="481" alt="image" src="https://github.com/user-attachments/assets/797f4276-d772-4d54-b94a-67bf6d1a17d8" />

## Syntax
### Init
To init the database, call the following
- Init(string location, Action<Exception, string?>? errorCallback = null)

### Definition
#### Creating tables
To create a table create an object that inherits from IDatabase_Table. This requires the table name to be set aswell as the getColums.
At the moment the datatypes are:
- TEXT
- BIT
- INTEGER
- DATETIME
- GUID
  
#### Creating migrations
To create a migration create an object that inherits from IDatabase_Migration. This requires the migration id (a number that determines the order of the migration, ascending) and the sql to run on up.

### Functions
#### Getting Data
You can use:
- GetItem<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? token = null) where T : IDatabase_Table
- GetItemsGeneric<T>(string sql, Func<SQLiteDataReader, Task<T>> deserializer, CancellationToken? cancellationToken = null)
- GetItems<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? token = null) where T : IDatabase_Table
- GetItemsWithCount<T>(string sql) where T : IDatabase_Table

- Exists<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? token = null) where T : IDatabase_Table

[System.Serializable]
public class DatabaseRoot
{
    public Database[] Databases;
}

[System.Serializable]
public class Database
{
    public string Name;
    public Schema[] Schemas;
}

[System.Serializable] 
public class Schema 
{
    public string Name; 
    public Table[] Tables;
}

[System.Serializable]
public class Table
{
    public string Name;
    public Column[] Columns;
}

[System.Serializable]
public class Column
{
    public string Name;
    public string DataType;
}
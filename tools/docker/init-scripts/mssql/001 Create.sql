IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SaaStack')
BEGIN
    CREATE DATABASE SaaStack;
END
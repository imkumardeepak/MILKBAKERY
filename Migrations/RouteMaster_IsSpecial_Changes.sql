-- SQL script for database changes introduced by migrations from November 19, 2025
-- Migration 1: 20251119125213_addspecial
-- Migration 2: 20251119125859_addspecialindex

-- Check if the IsSpecial column exists in RouteMaster table
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RouteMaster' 
    AND COLUMN_NAME = 'IsSpecial'
)
BEGIN
    -- Add the IsSpecial column to RouteMaster table
    ALTER TABLE [dbo].[RouteMaster]
    ADD [IsSpecial] [bit] NOT NULL DEFAULT 0
    
    PRINT 'Added IsSpecial column to RouteMaster table'
END
ELSE
BEGIN
    PRINT 'IsSpecial column already exists in RouteMaster table'
END

-- Check if the index on IsSpecial column exists
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_RouteMaster_IsSpecial' 
    AND object_id = OBJECT_ID('RouteMaster')
)
BEGIN
    -- Create index on IsSpecial column
    CREATE NONCLUSTERED INDEX [IX_RouteMaster_IsSpecial]
    ON [dbo].[RouteMaster] ([IsSpecial])
    
    PRINT 'Created index IX_RouteMaster_IsSpecial on RouteMaster table'
END
ELSE
BEGIN
    PRINT 'Index IX_RouteMaster_IsSpecial already exists on RouteMaster table'
END
-- 012: Add ProfilePicture column to AspNetUsers
IF COL_LENGTH('AspNetUsers', 'ProfilePicture') IS NULL
    ALTER TABLE AspNetUsers ADD ProfilePicture NVARCHAR(500) NULL;

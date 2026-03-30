INSERT INTO Countries (Name, ShortName)
SELECT 'Romania', 'RO'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'Romania'
);

INSERT INTO Countries (Name, ShortName)
SELECT 'France', 'FR'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'France'
);

INSERT INTO Countries (Name, ShortName)
SELECT 'Hungary', 'HU'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'Hungary'
);

INSERT INTO Countries (Name, ShortName)
SELECT 'Germany', 'DE'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'Germany'
);

INSERT INTO Countries (Name, ShortName)
SELECT 'Italy', 'IT'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'Italy'
);

INSERT INTO Countries (Name, ShortName)
SELECT 'Spain', 'ES'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'Spain'
);

INSERT INTO Countries (Name, ShortName)
SELECT 'United Kingdom', 'UK'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'United Kingdom'
);

INSERT INTO Countries (Name, ShortName)
SELECT 'Netherlands', 'NL'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'Netherlands'
);

INSERT INTO Countries (Name, ShortName)
SELECT 'Greece', 'GR'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'Greece'
);

INSERT INTO Countries (Name, ShortName)
SELECT 'Turkey', 'TR'
WHERE NOT EXISTS (
    SELECT 1 FROM Countries WHERE Name = 'Turkey'
);
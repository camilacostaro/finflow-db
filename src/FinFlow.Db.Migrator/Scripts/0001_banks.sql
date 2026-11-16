-- Tabela de referencia global
CREATE TABLE banks (
    id   smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code text NOT NULL UNIQUE CHECK (code ~ '^[a-z0-9_]+$'),
    name text NOT NULL
);

INSERT INTO banks (code, name) VALUES ('c6', 'C6 Bank');

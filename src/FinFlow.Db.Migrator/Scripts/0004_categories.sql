-- Tabela de referencia global
CREATE TABLE categories (
    id   smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code text NOT NULL UNIQUE CHECK (code ~ '^[a-z0-9_]+$'),
    name text NOT NULL
);

CREATE TABLE accounts (
    id           uuid PRIMARY KEY DEFAULT uuidv7(),
    workspace_id uuid NOT NULL REFERENCES workspaces (id),
    bank_id      smallint NOT NULL REFERENCES banks (id),
    name         text NOT NULL,
    created_at   timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, id)
);

CREATE TABLE statement_imports (
    id              uuid PRIMARY KEY DEFAULT uuidv7(),
    workspace_id    uuid NOT NULL REFERENCES workspaces (id),
    account_id      uuid NOT NULL,
    bank_id         smallint NOT NULL REFERENCES banks (id),
    file_hash       text NOT NULL,
    rows_total      integer NOT NULL CHECK (rows_total >= 0),
    rows_inserted   integer NOT NULL CHECK (rows_inserted >= 0),
    rows_duplicated integer NOT NULL CHECK (rows_duplicated >= 0),
    created_at      timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (workspace_id, account_id) REFERENCES accounts (workspace_id, id),
    UNIQUE (workspace_id, id),
    UNIQUE (workspace_id, file_hash)
);

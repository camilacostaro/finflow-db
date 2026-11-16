CREATE TABLE transactions (
    id           uuid PRIMARY KEY DEFAULT uuidv7(),
    workspace_id uuid NOT NULL REFERENCES workspaces (id),
    account_id   uuid NOT NULL,
    import_id    uuid,
    occurred_on  date NOT NULL,
    description  text NOT NULL,
    amount       numeric(19, 2) NOT NULL,
    category_id  smallint REFERENCES categories (id),
    dedup_key    text NOT NULL,
    created_at   timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (workspace_id, account_id) REFERENCES accounts (workspace_id, id),
    FOREIGN KEY (workspace_id, import_id) REFERENCES statement_imports (workspace_id, id),
    UNIQUE (workspace_id, account_id, dedup_key)
);

CREATE INDEX ix_transactions_workspace_occurred_on ON transactions (workspace_id, occurred_on);

CREATE TABLE users (
    id           uuid PRIMARY KEY DEFAULT uuidv7(),
    auth_subject text NOT NULL UNIQUE,
    email        text NOT NULL,
    created_at   timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE workspaces (
    id         uuid PRIMARY KEY DEFAULT uuidv7(),
    name       text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE workspace_members (
    workspace_id uuid NOT NULL REFERENCES workspaces (id) ON DELETE CASCADE,
    user_id      uuid NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    role         text NOT NULL CHECK (role IN ('owner', 'member')),
    PRIMARY KEY (workspace_id, user_id)
);

CREATE INDEX ix_workspace_members_user_id ON workspace_members (user_id);

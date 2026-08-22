
-- Connect to the shared application database.
\connect "AlfabetPretDb"

-- ============================================================
-- Users
-- ============================================================

CREATE ROLE cms_user
    WITH LOGIN PASSWORD 'cms_password';

CREATE ROLE api_user
    WITH LOGIN PASSWORD 'api_password';

-- ============================================================
-- Schemas
-- ============================================================

CREATE SCHEMA IF NOT EXISTS cms AUTHORIZATION cms_user;
CREATE SCHEMA IF NOT EXISTS api AUTHORIZATION api_user;

-- ============================================================
-- Database permissions
-- ============================================================

GRANT CONNECT, CREATE
    ON DATABASE "AlfabetPretDb"
    TO cms_user, api_user;

-- ============================================================
-- Schema permissions
-- ============================================================

GRANT USAGE, CREATE
    ON SCHEMA cms
    TO cms_user;

GRANT USAGE, CREATE
    ON SCHEMA api
    TO api_user;

-- ============================================================
-- Default search paths
-- ============================================================

ALTER ROLE cms_user
    IN DATABASE "AlfabetPretDb"
    SET search_path = cms, public;

ALTER ROLE api_user
    IN DATABASE "AlfabetPretDb"
    SET search_path = api, public;

-- ============================================================
-- Default privileges
-- ============================================================

ALTER DEFAULT PRIVILEGES
    IN SCHEMA cms
    GRANT SELECT, INSERT, UPDATE, DELETE
    ON TABLES
    TO cms_user;

ALTER DEFAULT PRIVILEGES
    IN SCHEMA api
    GRANT SELECT, INSERT, UPDATE, DELETE
    ON TABLES
    TO api_user;
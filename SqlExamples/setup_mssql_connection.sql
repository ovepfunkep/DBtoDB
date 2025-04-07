-- Install required extension
CREATE EXTENSION IF NOT EXISTS tds_fdw;

-- Create the foreign server connection
CREATE SERVER mssql_server
    FOREIGN DATA WRAPPER tds_fdw
    OPTIONS (
        servername '<<MSSQL_HOST>>', -- Replace with your MSSQL server host
        database '<<MSSQL_DB>>',     -- Replace with your MSSQL database name
        tds_version '7.4',           -- Use appropriate TDS version for your SQL Server
        msg_handler 'notice'         -- Helpful for debugging
    );

-- Create user mapping
CREATE USER MAPPING FOR CURRENT_USER
    SERVER mssql_server
    OPTIONS (
        username '<<MSSQL_USER>>',   -- Replace with your MSSQL username
        password '<<MSSQL_PASS>>'    -- Replace with your MSSQL password
    );

-- Create a function to execute MSSQL stored procedures
CREATE OR REPLACE FUNCTION exec_mssql_proc(
    proc_name text,
    params json DEFAULT '{}'::json
) RETURNS TABLE (result_data json) AS $$
DECLARE
    param_list text := '';
    param_values text := '';
    query text;
    param_name text;
    param_value text;
    temp_table_name text;
BEGIN
    -- Generate a unique temporary table name using session pid and a random suffix
    temp_table_name := format('temp_proc_results_%s_%s', 
                            pg_backend_pid(), 
                            floor(random() * 1000000)::int);

    -- Build parameter list from JSON
    FOR param_name, param_value IN SELECT * FROM json_each_text(params)
    LOOP
        IF param_list != '' THEN
            param_list := param_list || ', ';
            param_values := param_values || ', ';
        END IF;
        param_list := param_list || '@' || param_name;
        param_values := param_values || quote_nullable(param_value);
    END LOOP;

    -- Create foreign table for the stored procedure results
    -- Using session-specific temporary table
    EXECUTE format('DROP FOREIGN TABLE IF EXISTS %I', temp_table_name);
    
    -- Construct the query to execute the stored procedure
    query := format('
        CREATE FOREIGN TABLE %I (
            result_data json
        )
        SERVER mssql_server
        OPTIONS (
            query $q$
            DECLARE %s;
            EXEC %s %s;
            $q$
        )',
        temp_table_name,
        CASE WHEN param_list != '' 
             THEN param_list || ' varchar(max) = ' || param_values 
             ELSE ''
        END,
        proc_name,
        param_list
    );

    -- Create the foreign table
    EXECUTE query;

    -- Return results using dynamic SQL to handle the dynamic table name
    RETURN QUERY EXECUTE format('SELECT * FROM %I', temp_table_name);

    -- Cleanup
    EXECUTE format('DROP FOREIGN TABLE IF EXISTS %I', temp_table_name);

EXCEPTION WHEN OTHERS THEN
    -- Ensure cleanup happens even if there's an error
    EXECUTE format('DROP FOREIGN TABLE IF EXISTS %I', temp_table_name);
    RAISE;
END;
$$ LANGUAGE plpgsql;

-- Example usage comment:
/*
-- To call an MSSQL stored procedure named 'GetCustomerDetails' with parameters:
SELECT * FROM exec_mssql_proc(
    'GetCustomerDetails',
    '{"CustomerID": "12345", "IncludeOrders": "1"}'::json
);
*/

-- Add helpful comments
COMMENT ON FUNCTION exec_mssql_proc(text, json) IS 
'Executes an MSSQL stored procedure with JSON parameters and returns results as JSON.
Parameters:
- proc_name: Name of the MSSQL stored procedure to execute
- params: JSON object containing parameter names and values

Example:
SELECT * FROM exec_mssql_proc(
    ''GetCustomerDetails'',
    ''{"CustomerID": "12345", "IncludeOrders": "1"}''::json
);'; 
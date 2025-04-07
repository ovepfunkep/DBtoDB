-- Enable the http extension if not already enabled
CREATE EXTENSION IF NOT EXISTS http;

-- Create a function to execute SQL Server stored procedures via the Web API
CREATE OR REPLACE FUNCTION exec_sqlserver_proc(
    proc_name text,                  -- Name of the SQL Server stored procedure
    params jsonb DEFAULT '{}',       -- Parameters as JSONB
    use_cache boolean DEFAULT false, -- Whether to use caching
    cache_minutes int DEFAULT NULL   -- Cache duration in minutes
)
RETURNS jsonb
LANGUAGE plpgsql
AS $$
DECLARE
    api_url text := 'http://your-api-host:port/api/StoredProcedure/execute'; -- Replace with your actual API URL
    api_response http_response;
    request_body jsonb;
BEGIN
    -- Input validation
    IF proc_name IS NULL OR proc_name = '' THEN
        RAISE EXCEPTION 'Procedure name cannot be null or empty';
    END IF;

    -- Construct the request body
    request_body := jsonb_build_object(
        'procedureName', proc_name,
        'parameters', params,
        'useCache', use_cache,
        'cacheMinutes', cache_minutes
    );

    -- Log the request (optional)
    RAISE NOTICE 'Calling SQL Server procedure % with parameters %', proc_name, params;

    -- Make the HTTP POST request to the API
    SELECT *
    INTO api_response
    FROM http_post(
        url := api_url,
        body := request_body::text,
        content_type := 'application/json'
    );

    -- Check for HTTP errors
    IF api_response.status != 200 THEN
        RAISE EXCEPTION 'API request failed with status %: %', 
            api_response.status, 
            api_response.content::json->'error';
    END IF;

    -- Return the results
    RETURN api_response.content::jsonb;

EXCEPTION
    WHEN others THEN
        -- Add context to the error
        RAISE EXCEPTION 'Error executing SQL Server procedure %: %', proc_name, SQLERRM;
END;
$$;

-- Add helpful function comment
COMMENT ON FUNCTION exec_sqlserver_proc(text, jsonb, boolean, int) IS 
'Executes a SQL Server stored procedure through the Web API.

Parameters:
- proc_name: Name of the stored procedure to execute (required)
- params: JSONB object containing procedure parameters (optional)
- use_cache: Whether to cache the results (optional, default false)
- cache_minutes: How long to cache the results in minutes (optional)

Example usage:
SELECT * FROM exec_sqlserver_proc(
    ''GetCustomerDetails'',
    ''{"CustomerID": "12345", "IncludeOrders": true}''::jsonb,
    true,
    5
);';

-- Example of how to handle the results with different data types
CREATE OR REPLACE FUNCTION get_customer_details(
    customer_id text,
    include_orders boolean DEFAULT false
)
RETURNS TABLE (
    customer_name text,
    email text,
    total_orders int
)
LANGUAGE plpgsql
AS $$
DECLARE
    result jsonb;
BEGIN
    -- Call the SQL Server procedure through our wrapper function
    result := exec_sqlserver_proc(
        'GetCustomerDetails',
        jsonb_build_object(
            'CustomerID', customer_id,
            'IncludeOrders', include_orders
        )
    );

    -- Parse the results into our return table
    RETURN QUERY
    SELECT 
        (r->>'CustomerName')::text as customer_name,
        (r->>'Email')::text as email,
        (r->>'TotalOrders')::int as total_orders
    FROM jsonb_array_elements(result) r;
END;
$$;

-- Add helpful comment for the wrapper function
COMMENT ON FUNCTION get_customer_details(text, boolean) IS
'Gets customer details from SQL Server including optional order information.

Parameters:
- customer_id: The ID of the customer to look up
- include_orders: Whether to include order information (default false)

Example:
SELECT * FROM get_customer_details(''12345'', true);';

-- Example of how to use the functions
/*
-- Basic usage with no parameters:
SELECT * FROM exec_sqlserver_proc('GetAllCustomers');

-- With parameters and caching:
SELECT * FROM exec_sqlserver_proc(
    'GetCustomerDetails',
    '{"CustomerID": "12345", "IncludeOrders": true}'::jsonb,
    true,
    5
);

-- Using the typed wrapper function:
SELECT * FROM get_customer_details('12345', true);
*/ 
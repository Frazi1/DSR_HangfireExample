for i in {1..50};
do
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Your_password123 -d master -Q "CREATE DATABASE hangfire GO;"
    if [ $? -eq 0 ]
    then
        echo "hangfire db created completed"
        break
    else
        echo "not ready yet..."
        sleep 1
    fi
done
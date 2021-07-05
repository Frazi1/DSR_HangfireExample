FROM mcr.microsoft.com/azure-sql-edge:latest AS db

COPY setup.sh setup.sh
COPY entrypoint.sh entrypoint.sh

CMD /bin/bash ./entrypoint.sh

FROM python:3.12

COPY ./Set-up/azurite .

RUN pip install azure-storage-blob==12.22.0
RUN pip install azure-storage-queue

CMD python3 -u ./azurite-setup.py
# Create a non-root user and set permissions
RUN adduser --disabled-password --gecos '' nonrootuser
# Switch to non-root user
USER nonrootuser

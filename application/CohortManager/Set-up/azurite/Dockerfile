FROM python:3.12

COPY ./Set-up/azurite .
COPY ./rules ./rules

RUN pip install azure-storage-blob

CMD python3 -u ./azurite-setup.py

FROM python:3.12

COPY . .

RUN pip install azure-storage-blob

CMD python3 -u ./azurite-setup.py

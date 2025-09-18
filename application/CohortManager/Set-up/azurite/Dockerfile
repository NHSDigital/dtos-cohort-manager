FROM python:3.12

COPY ./Set-up/azurite .

RUN pip install azure-storage-blob==12.22.0 \
&& pip install azure-storage-queue

CMD ["python3", "-u", "./azurite-setup.py"]

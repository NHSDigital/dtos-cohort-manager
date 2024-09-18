# Use an official Python runtime as a parent image
FROM python:3.9-slim

# Set the working directory in the container
WORKDIR /usr/src/app

# Copy the current directory contents into the container at /usr/src/app
COPY . .

# Install the required Python packages
RUN pip install Flask

# Make port 5000 available to the world outside this container
EXPOSE 5000

# Run the Flask app
CMD ["python",  "./ControllerTest.py"]

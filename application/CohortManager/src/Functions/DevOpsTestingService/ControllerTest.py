# hello.py
from flask import Flask, jsonify

app = Flask(__name__)

@app.route('/api/hello', methods=['GET'])
def hello_world():
    return jsonify(message="Hello, World again!")

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)

from pymongo import MongoClient

# Konfigurasi koneksi MongoDB
MONGO_URI = "mongodb://localhost:27017/"
DATABASE_NAME = "GrowTrack"

def get_database():
    """
    Menghubungkan ke database MongoDB dan mengembalikan instance database.
    """
    client = MongoClient(MONGO_URI)
    return client[DATABASE_NAME]

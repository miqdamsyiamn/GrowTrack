from flask import Flask, jsonify, request
from Config.config import get_database
from datetime import datetime
import time

app = Flask(__name__)

# Koneksi ke database
db = get_database()
user_collection = db.User  # Koleksi untuk user
plant_collection = db.DataPlant  # Koleksi untuk data tanaman

# Endpoint untuk mendapatkan semua data pengguna**
@app.route('/users', methods=['GET'])
def get_users():
    try:
        users = list(user_collection.find({}, {"_id": 0}))  # Mengambil data tanpa _id
        return jsonify(users), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Endpoint untuk menambahkan pengguna baru**
@app.route('/users', methods=['POST'])
def add_user():
    try:
        data = request.json
        if not data:
            return jsonify({"error": "Data tidak boleh kosong"}), 400

        user_collection.insert_one(data)
        return jsonify({"message": "User berhasil ditambahkan"}), 201
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Endpoint untuk login**
@app.route('/login', methods=['POST'])
def login():
    try:
        data = request.json
        username = data.get("username")
        password = data.get("password")

        if not username or not password:
            return jsonify({"error": "Username dan password tidak boleh kosong"}), 400

        user = user_collection.find_one({"username": username, "password": password}, {"_id": 0})

        if user:
            return jsonify({"message": "Login berhasil", "user": user}), 200
        else:
            return jsonify({"error": "Username atau password salah"}), 401
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Endpoint untuk mengecek apakah user sudah pernah input data tanaman**
@app.route('/check_first_input', methods=['GET'])
def check_first_input():
    try:
        username = request.args.get("username")
        if not username:
            return jsonify({"error": "Username harus disertakan"}), 400

        data = plant_collection.find_one({"username": username}, {"_id": 0, "date": 1})

        if data:
            return jsonify({"status": "exists", "first_input_date": data["date"]}), 200
        else:
            return jsonify({"status": "not_found"}), 404
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Endpoint untuk mendapatkan daftar tanaman unik per user**
@app.route('/plant_names', methods=['GET'])
def get_plant_names():
    try:
        username = request.args.get("username")
        if not username:
            return jsonify({"error": "Username harus disertakan"}), 400

        plants = plant_collection.distinct("name", {"username": username})
        return jsonify(plants), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Endpoint untuk mendapatkan data pertumbuhan tanaman dengan tanggal yang diformat**
@app.route('/plant_growth', methods=['GET'])
def get_plant_growth():
    try:
        username = request.args.get("username")
        plant_name = request.args.get("name")

        if not username or not plant_name:
            return jsonify({"error": "Username dan nama tanaman harus disertakan"}), 400

        data = list(plant_collection.find(
            {"username": username, "name": plant_name},
            {"_id": 0, "date": 1, "plant_height": 1}
        ).sort("date", 1))

        for item in data:
            if "date" in item:
                try:
                    parsed_date = datetime.strptime(item["date"], "%Y-%m-%d")
                    item["date"] = parsed_date.strftime("%Y-%m-%d")
                except ValueError:
                    item["date"] = None

        return jsonify(data), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Endpoint untuk mendapatkan semua data tanaman milik user tertentu**
@app.route('/input', methods=['GET'])
def get_data_plant():
    try:
        username = request.args.get("username")
        if not username:
            return jsonify({"error": "Username harus disertakan"}), 400

         # Simulasi delay untuk memastikan data tersimpan sebelum diambil
        time.sleep(0.5)

        data = list(plant_collection.find({"username": username}, {"_id": 0}))

        for item in data:
            if "date" in item:
                try:
                    parsed_date = datetime.strptime(item["date"], "%Y-%m-%d")
                    item["date"] = parsed_date.strftime("%Y-%m-%d")
                except ValueError:
                    item["date"] = None

            if "status" not in item:
                item["status"] = "Belum Panen"

        return jsonify(data), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Endpoint untuk menambahkan data tanaman dengan status**
@app.route('/input', methods=['POST'])
def add_data_plant():
    try:
        data = request.json
        username = data.get("username")

        if not data or not username:
            return jsonify({"error": "Data tidak boleh kosong dan harus memiliki username"}), 400

        try:
            parsed_date = datetime.strptime(data.get("date"), "%Y-%m-%d")
            formatted_date = parsed_date.strftime("%Y-%m-%d")
        except ValueError:
            return jsonify({"error": "Format tanggal tidak valid, harus YYYY-MM-DD"}), 400

        status = data.get("status", "Belum Panen")  # Default: Belum Panen

        plant_data = {
            "username": username,
            "name": data.get("name"),
            "plant_height": float(data.get("plant_height")),
            "leaf_condition": data.get("leaf_condition"),
            "water_demand": data.get("water_demand"),
            "date": formatted_date,
            "status": status
        }

        plant_collection.insert_one(plant_data)
        return jsonify({"message": "Data berhasil ditambahkan"}), 201
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Endpoint untuk Edit Data Tanaman dengan Debugging**
@app.route('/edit', methods=['PUT'])
def edit():
    try:
        data = request.json
        username = data.get("username")
        original_name = data.get("original_name")
        original_date = data.get("original_date")
        original_plant_height = data.get("original_plant_height")

        if not username or not original_name or not original_date or not original_plant_height:
            return jsonify({"error": "Username, nama tanaman, tanggal, dan tinggi awal harus disertakan"}), 400

        # 🔹 Debugging: Cek parameter yang dikirim
        print(f"[DEBUG] Mencari data dengan:")
        print(f"   Username: {username}")
        print(f"   Nama Awal: {original_name}")
        print(f"   Tanggal Awal: {original_date}")
        print(f"   Tinggi Awal: {original_plant_height}")

        # 🔹 Pastikan original_plant_height dikonversi ke float agar sesuai dengan database
        query = {
            "username": username,
            "name": original_name,
            "date": original_date,
            "plant_height": float(original_plant_height)
        }

        existing_data = plant_collection.find_one(query)

        if not existing_data:
            return jsonify({"error": "Data tidak ditemukan"}), 404

        update_fields = {
            "name": data.get("name", existing_data["name"]),
            "plant_height": float(data.get("plant_height", existing_data["plant_height"])),  
            "leaf_condition": data.get("leaf_condition", existing_data["leaf_condition"]),
            "water_demand": data.get("water_demand", existing_data["water_demand"]),
            "date": data.get("date", existing_data["date"]),
            "status": data.get("status", existing_data["status"])
        }

        result = plant_collection.update_one(query, {"$set": update_fields})

        if result.matched_count == 0:
            return jsonify({"error": "Data tidak ditemukan untuk diperbarui"}), 404

        time.sleep(0.3)

        return jsonify({"message": "Data berhasil diperbarui"}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500



# Endpoint untuk Hapus Data Tanaman**
@app.route('/delete', methods=['DELETE'])
def delete():
    try:
        username = request.args.get("username")
        date = request.args.get("date")

        if not username or not date:
            return jsonify({"error": "Username dan tanggal harus disertakan"}), 400

        existing_data = plant_collection.find_one({"username": username, "date": date})
        if not existing_data:
            return jsonify({"error": "Data tidak ditemukan"}), 404

        plant_collection.delete_one({"username": username, "date": date})
        return jsonify({"message": "Data berhasil dihapus"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

#bagian reporting
@app.route('/plant_history', methods=['GET'])
def get_plant_history():
    try:
        username = request.args.get("username")
        plant_name = request.args.get("name")

        if not username or not plant_name:
            return jsonify({"error": "Username dan nama tanaman harus disertakan"}), 400

        data = list(plant_collection.find(
            {"username": username, "name": plant_name},
            {"_id": 0, "name": 1, "plant_height": 1, "leaf_condition": 1, "water_demand": 1, "date": 1, "status": 1}
        ).sort("date", 1))  # Sortir berdasarkan tanggal

        return jsonify(data), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


if __name__ == '__main__':
    app.run(debug=True, port=5000)

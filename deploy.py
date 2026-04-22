import os
import shutil
import subprocess
import sys

# === CONFIG ===
ROOT = os.path.dirname(os.path.abspath(__file__))
REACT_DIR = os.path.join(ROOT, "CaseManagementUI")
DOTNET_DIR = os.path.join(ROOT, "WebAppMulti")
WWWROOT = os.path.join(DOTNET_DIR, "wwwroot")

# ===============================

def run(cmd, cwd=None):
    print(f"\n▶ {cmd}")
    result = subprocess.run(cmd, cwd=cwd, shell=True)
    if result.returncode != 0:
        print("❌ Command failed")
        sys.exit(1)

def clean_wwwroot():
    print("\n🧹 Cleaning wwwroot...")
    if os.path.exists(WWWROOT):
        for item in os.listdir(WWWROOT):
            path = os.path.join(WWWROOT, item)
            if os.path.isdir(path):
                shutil.rmtree(path)
            else:
                os.remove(path)
    else:
        os.makedirs(WWWROOT)

def copy_dist():
    dist = os.path.join(REACT_DIR, "dist")
    if not os.path.exists(dist):
        print("❌ React dist folder not found. Build failed?")
        sys.exit(1)

    print("\n📦 Copying React build to wwwroot...")
    shutil.copytree(dist, WWWROOT, dirs_exist_ok=True)

def main():
    print("🚀 Starting LOCAL PROD deploy")

    # 1️⃣ Build React
    run("npm install", cwd=REACT_DIR)
    run("npm run build", cwd=REACT_DIR)

    # 2️⃣ Deploy to ASP.NET
    clean_wwwroot()
    copy_dist()

    print("\n✅ React deployed to ASP.NET wwwroot")

    # 3️⃣ Optional: start ASP.NET
    start = input("\n▶ Start ASP.NET Core now? (y/n): ").lower()
    if start == "y":
        run("dotnet run", cwd=DOTNET_DIR)

    print("\n🎉 Done!")

if __name__ == "__main__":
    main()

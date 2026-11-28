import hashlib
import json
import shutil
import sys
from pathlib import Path
from urllib.parse import urlsplit


tag, artifacts, output, *uploaded = sys.argv[1:]
version = tag[1:]
artifacts, output = Path(artifacts), Path(output)
prefix = f"https://github.com/NagiYume/Starward/releases/download/{tag}/"
output.mkdir(exist_ok=True)


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha256(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def asset(url, size=None, checksum=None):
    name = urlsplit(url).path.rsplit("/", 1)[-1]
    require(url == prefix + name, f"URL outside this release: {url}")
    path = output / name
    require(path.is_file(), f"Missing asset: {name}")
    if size is not None:
        require(path.stat().st_size == size, f"Incorrect size: {name}")
    if checksum is not None:
        require(sha256(path) == checksum, f"Incorrect checksum: {name}")
    return path


if uploaded:
    remote = {item["name"]: item for item in json.loads(Path(uploaded[0]).read_text())["assets"]}
    require(set(remote) == {path.name for path in output.iterdir()}, "Incorrect set of uploaded assets")
    for name, item in remote.items():
        path = output / name
        require(item["state"] == "uploaded" and item["size"] == path.stat().st_size
                and item["digest"] == "sha256:" + sha256(path), f"Incorrect upload: {name}")
    sys.exit(0)

releases = {}
for architecture in ("x64", "arm64"):
    files = [path for path in (artifacts / f"release-{architecture}").rglob("*") if path.is_file()]
    for path in files:
        if path.name == f"release_info_{version}.json":
            info = json.loads(path.read_text(encoding="utf-8-sig"))
            require(info["version"] == version, f"Incorrect release version: {path}")
            require(set(info["releases"]) == {f"{architecture}-setup", f"{architecture}-portable"},
                    f"Incorrect architecture metadata: {path}")
            releases.update(info["releases"])
            continue
        destination = output / path.name
        if destination.exists():
            require(sha256(destination) == sha256(path), f"Conflicting assets named {path.name}")
        else:
            shutil.copyfile(path, destination)

require(len(releases) == 4, "Release metadata is incomplete")
checked_ranges = set()
checked_patches = set()


def check_manifest(url, architecture, install_type, old_version=None):
    manifest = json.loads(asset(url).read_text(encoding="utf-8-sig"))
    require(manifest["version"] == version and manifest.get("diff_version") == old_version
            and manifest["architecture"].lower() == architecture
            and manifest["install_type"].lower() == install_type, f"Incorrect manifest: {url}")
    require(manifest["file_count"] == len(manifest["files"]) > 0, f"Missing files: {url}")
    bundle = asset(manifest["file_bundle_url"])
    bundle_size = bundle.stat().st_size
    with bundle.open("rb") as stream:
        for file in manifest["files"]:
            start, length = file["bundle_offset"], file["compressed_size"]
            key = (bundle, start, length, file["compressed_hash"])
            if key not in checked_ranges:
                require(0 <= start < start + length <= bundle_size, f"Invalid range: {file['path']}")
                stream.seek(start)
                digest = hashlib.sha256(stream.read(length)).hexdigest()
                require(digest == file["compressed_hash"], f"Invalid compressed file: {file['path']}")
                checked_ranges.add(key)
            patch = file.get("patch")
            if patch and patch.get("id"):
                patch_url = manifest["url_prefix"] + patch["id"] + (patch.get("url_suffix") or "")
                patch_key = (patch_url, patch["patch_size"], patch["patch_hash"])
                if patch_key not in checked_patches:
                    asset(*patch_key)
                    checked_patches.add(patch_key)
                require(0 <= patch.get("offset", 0) < patch.get("offset", 0) + patch["length"] <= patch["patch_size"],
                        f"Invalid patch range: {file['path']}")


for key, detail in releases.items():
    architecture, install_type = key.split("-")
    require(detail["version"] == version, f"Incorrect release version: {key}")
    asset(detail["package_url"], detail["package_size"], detail["package_hash"])
    if install_type == "setup":
        setup = detail["setup"]
        asset(setup["url"], setup["size"], setup["hash"])
    check_manifest(detail["manifest_url"], architecture, install_type)
    for previous, diff in detail.get("diffs", {}).items():
        require(previous == diff["diff_version"] and previous != version, f"Incorrect baseline: {previous}")
        check_manifest(diff["manifest_url"], architecture, install_type, previous)

(output / "release-info.json").write_text(json.dumps({"version": version, "releases": releases}, indent=2) + "\n", encoding="utf-8")

#!/usr/bin/env python3
import argparse
import base64
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
from typing import Dict, List, Optional, Tuple


SPOTIFY_TOKEN_URL = "https://accounts.spotify.com/api/token"
SPOTIFY_PLAYLIST_URL = "https://api.spotify.com/v1/playlists/{playlist_id}/tracks"
DEEZER_SEARCH_URL = "https://api.deezer.com/search"


def get_spotify_token(client_id: str, client_secret: str) -> str:
	auth_bytes = f"{client_id}:{client_secret}".encode("utf-8")
	auth_header = base64.b64encode(auth_bytes).decode("ascii")

	data = urllib.parse.urlencode({"grant_type": "client_credentials"}).encode("utf-8")
	req = urllib.request.Request(
		SPOTIFY_TOKEN_URL,
		data=data,
		headers={
			"Authorization": f"Basic {auth_header}",
			"Content-Type": "application/x-www-form-urlencoded",
		},
		method="POST",
	)
	with urllib.request.urlopen(req, timeout=30) as resp:
		payload = json.loads(resp.read().decode("utf-8"))
	token = payload.get("access_token")
	if not token:
		raise RuntimeError("Spotify access token not found in response")
	return token


def fetch_spotify_tracks(playlist_id: str, token: str, verbose: bool) -> List[Dict]:
	tracks: List[Dict] = []
	url = SPOTIFY_PLAYLIST_URL.format(playlist_id=playlist_id)
	params = {"limit": 100}

	while url:
		if params:
			url_with_params = f"{url}?{urllib.parse.urlencode(params)}"
		else:
			url_with_params = url

		req = urllib.request.Request(
			url_with_params,
			headers={"Authorization": f"Bearer {token}"},
			method="GET",
		)
		with urllib.request.urlopen(req, timeout=30) as resp:
			data = json.loads(resp.read().decode("utf-8"))
		items = data.get("items", [])
		for item in items:
			track = item.get("track")
			if track:
				tracks.append(track)

		if verbose:
			print(f"Fetched {len(tracks)} tracks...", flush=True)

		url = data.get("next")
		params = None

	return tracks


def build_search_query(track: Dict) -> Optional[str]:
	name = track.get("name")
	artists = track.get("artists") or []
	if not name or not artists:
		return None
	artist_name = artists[0].get("name")
	if not artist_name:
		return None
	return f'"{name}" "{artist_name}"'


def search_deezer_track_id(query: str) -> Optional[int]:
	params = urllib.parse.urlencode({"q": query, "limit": 1})
	url = f"{DEEZER_SEARCH_URL}?{params}"
	req = urllib.request.Request(url, method="GET")
	with urllib.request.urlopen(req, timeout=30) as resp:
		data = json.loads(resp.read().decode("utf-8"))
	results = data.get("data") or []
	if not results:
		return None
	return results[0].get("id")


def map_spotify_to_deezer_ids(tracks: List[Dict], verbose: bool) -> Tuple[List[int], List[str]]:
	deezer_ids: List[int] = []
	missing: List[str] = []

	for index, track in enumerate(tracks, start=1):
		query = build_search_query(track)
		track_name = track.get("name") or "<unknown>"
		if not query:
			missing.append(track_name)
			continue

		try:
			deezer_id = search_deezer_track_id(query)
		except (urllib.error.URLError, urllib.error.HTTPError):
			missing.append(track_name)
			continue

		if deezer_id is None:
			missing.append(track_name)
			continue

		deezer_ids.append(deezer_id)

		if verbose and index % 10 == 0:
			print(f"Mapped {index}/{len(tracks)} tracks...", flush=True)

	return deezer_ids, missing


def parse_args() -> argparse.Namespace:
	parser = argparse.ArgumentParser(
		description="Map a Spotify playlist to Deezer track IDs.",
	)
	parser.add_argument("playlist_id", help="Spotify playlist ID")
	parser.add_argument(
		"--output-dir",
		default=".",
		help="Directory for output JSON file (default: current directory)",
	)
	parser.add_argument(
		"--verbose",
		action="store_true",
		help="Print progress while fetching and mapping tracks",
	)
	return parser.parse_args()


def main() -> int:
	args = parse_args()
	playlist_id = args.playlist_id.strip()
	if not playlist_id:
		print("Playlist ID is required", file=sys.stderr)
		return 1

	client_id = os.environ.get("SPOTIFY_CLIENT_ID")
	client_secret = os.environ.get("SPOTIFY_CLIENT_SECRET")
	if not client_id or not client_secret:
		print("Missing SPOTIFY_CLIENT_ID or SPOTIFY_CLIENT_SECRET", file=sys.stderr)
		return 1

	try:
		token = get_spotify_token(client_id, client_secret)
		if args.verbose:
			print("Fetching Spotify tracks...", flush=True)
		tracks = fetch_spotify_tracks(playlist_id, token, args.verbose)
	except (urllib.error.URLError, urllib.error.HTTPError) as exc:
		print(f"Spotify API error: {exc}", file=sys.stderr)
		return 1

	if args.verbose:
		print("Searching Deezer...", flush=True)
	deezer_ids, missing = map_spotify_to_deezer_ids(tracks, args.verbose)

	output_path = os.path.join(args.output_dir, f"{playlist_id}.json")
	with open(output_path, "w", encoding="utf-8") as handle:
		json.dump(deezer_ids, handle, indent=2)

	print(f"Wrote {len(deezer_ids)} Deezer IDs to {output_path}")
	if missing:
		print(f"Missing {len(missing)} tracks on Deezer")
	return 0


if __name__ == "__main__":
	raise SystemExit(main())

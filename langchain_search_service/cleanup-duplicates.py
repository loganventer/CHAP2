#!/usr/bin/env python3
"""
Script to clean up duplicate entries in the Qdrant chorus collection.
This script will:
1. Fetch all points from the collection
2. Identify duplicates based on the 'id' field
3. Keep only the first occurrence of each unique chorus
4. Delete the duplicates
"""

import requests
import json
from collections import defaultdict
from typing import List, Dict, Any

QDRANT_URL = "http://localhost:6333"
COLLECTION_NAME = "chorus-vectors"

def get_all_points() -> List[Dict[str, Any]]:
    """Fetch all points from the collection"""
    url = f"{QDRANT_URL}/collections/{COLLECTION_NAME}/points/scroll"
    params = {
        "limit": 1000,  # Get all points
        "with_payload": True,
        "with_vector": False
    }
    
    response = requests.post(url, json=params)
    response.raise_for_status()
    
    data = response.json()
    return data["result"]["points"]

def identify_duplicates(points: List[Dict[str, Any]]) -> Dict[str, List[str]]:
    """Identify duplicate choruses based on the 'id' field"""
    duplicates = defaultdict(list)
    
    for point in points:
        payload = point.get("payload", {})
        metadata = payload.get("metadata", {})
        chorus_id = metadata.get("id")  # Look in metadata, not payload root
        point_id = point.get("id")
        
        if chorus_id:
            duplicates[chorus_id].append(point_id)
    
    # Filter to only those with duplicates
    return {k: v for k, v in duplicates.items() if len(v) > 1}

def delete_points(point_ids: List[str]) -> bool:
    """Delete specific points from the collection"""
    url = f"{QDRANT_URL}/collections/{COLLECTION_NAME}/points/delete"
    payload = {"points": point_ids}
    
    response = requests.post(url, json=payload)
    response.raise_for_status()
    
    return True

def main():
    print("🔍 Fetching all points from Qdrant collection...")
    
    try:
        points = get_all_points()
        print(f"📊 Found {len(points)} total points")
        
        duplicates = identify_duplicates(points)
        print(f"🔍 Found {len(duplicates)} choruses with duplicates")
        
        if not duplicates:
            print("✅ No duplicates found!")
            return
        
        total_duplicates_to_remove = 0
        points_to_delete = []
        
        for chorus_id, point_ids in duplicates.items():
            # Keep the first occurrence, delete the rest
            points_to_keep = point_ids[0]
            points_to_remove = point_ids[1:]
            
            print(f"🎵 Chorus ID {chorus_id}:")
            print(f"   Keeping: {points_to_keep}")
            print(f"   Removing: {len(points_to_remove)} duplicates")
            
            points_to_delete.extend(points_to_remove)
            total_duplicates_to_remove += len(points_to_remove)
        
        if points_to_delete:
            print(f"\n🗑️  Deleting {total_duplicates_to_remove} duplicate points...")
            
            # Delete in batches to avoid overwhelming the API
            batch_size = 100
            for i in range(0, len(points_to_delete), batch_size):
                batch = points_to_delete[i:i + batch_size]
                delete_points(batch)
                print(f"   Deleted batch {i//batch_size + 1}/{(len(points_to_delete) + batch_size - 1)//batch_size}")
            
            print("✅ Duplicate cleanup completed!")
            
            # Verify the cleanup
            remaining_points = get_all_points()
            print(f"📊 Remaining points: {len(remaining_points)}")
            
        else:
            print("✅ No duplicates to remove!")
            
    except Exception as e:
        print(f"❌ Error: {e}")
        return False
    
    return True

if __name__ == "__main__":
    main() 
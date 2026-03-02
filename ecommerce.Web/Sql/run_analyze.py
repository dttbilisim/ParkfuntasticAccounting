#!/usr/bin/env python3
import psycopg2
import sys

# Database connection
conn = psycopg2.connect(
    host="92.204.172.6",
    port=5454,
    database="MarketPlace",
    user="myinsurer",
    password="Posmdh0738"
)
conn.set_session(autocommit=True)
cursor = conn.cursor()

# Set to capture NOTICE messages
cursor.execute("SET client_min_messages TO NOTICE")

# Read and execute SQL file
with open('analyze_product_duplicates.sql', 'r') as f:
    sql_content = f.read()

# Remove \echo commands and comments, split by semicolons
import re

# Remove \echo lines
sql_content = re.sub(r'\\echo.*\n', '', sql_content)
# Remove comment-only lines
sql_content = re.sub(r'^\s*--.*\n', '', sql_content, flags=re.MULTILINE)

# Split by semicolons and execute each statement
statements = [s.strip() for s in sql_content.split(';') if s.strip() and not s.strip().startswith('--')]

for i, statement in enumerate(statements, 1):
    if not statement:
        continue
    
    # Skip DO blocks (they have their own semicolons)
    if statement.startswith('DO $$'):
        continue
    
    # Skip empty or comment-only statements
    if not statement or statement.startswith('--'):
        continue
    
    try:
        print(f"\n{'='*70}")
        print(f"Query {i}: {statement[:100].replace(chr(10), ' ')}...")
        print(f"{'='*70}")
        
        cursor.execute(statement)
        
        # Fetch and print results
        if cursor.description:
            columns = [desc[0] for desc in cursor.description]
            rows = cursor.fetchall()
            
            # Print header
            print(" | ".join(columns))
            print("-" * 70)
            
            # Print rows
            for row in rows:
                print(" | ".join(str(val) if val is not None else "NULL" for val in row))
            
            print(f"\n✅ {len(rows)} row(s) returned")
        else:
            print("✅ Query executed successfully")
            
    except Exception as e:
        print(f"❌ Error: {e}")
        continue

cursor.close()
conn.close()

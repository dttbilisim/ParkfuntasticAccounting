import sys

def count_tags(filename):
    with open(filename, 'r') as f:
        content = f.read()
    
    div_open = content.count('<div')
    div_close = content.count('</div>')
    brace_open = content.count('{')
    brace_close = content.count('}')
    
    # Filter for Razor braces (not in JS/style)
    # This is rough but helps
    
    print(f"Div Open: {div_open}, Div Close: {div_close}")
    print(f"Brace Open: {brace_open}, Brace Close: {brace_close}")

if __name__ == '__main__':
    count_tags(sys.argv[1])

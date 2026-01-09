# Code Efficiency Guide: Identifying and Improving Slow Code

## Table of Contents
1. [Introduction](#introduction)
2. [Common Performance Issues](#common-performance-issues)
3. [Profiling Tools](#profiling-tools)
4. [Optimization Techniques](#optimization-techniques)
5. [Best Practices](#best-practices)
6. [Language-Specific Tips](#language-specific-tips)

## Introduction

This guide helps you identify and fix slow or inefficient code. Performance optimization should be done after correctness is ensured, following the principle: "Make it work, make it right, make it fast."

## Common Performance Issues

### 1. Algorithm Complexity Problems

**Bad Example (O(n²)):**
```python
# Inefficient: Nested loops for checking duplicates
def has_duplicates_slow(items):
    for i in range(len(items)):
        for j in range(i + 1, len(items)):
            if items[i] == items[j]:
                return True
    return False
```

**Good Example (O(n)):**
```python
# Efficient: Use set for O(1) lookup
def has_duplicates_fast(items):
    return len(items) != len(set(items))
```

### 2. Unnecessary Database Queries

**Bad Example:**
```python
# N+1 query problem
for user in User.all():
    print(user.profile.name)  # Each iteration makes a DB query
```

**Good Example:**
```python
# Use eager loading/joins
users = User.all().prefetch_related('profile')
for user in users:
    print(user.profile.name)
```

### 3. Memory Inefficiency

**Bad Example:**
```python
# Loading entire file into memory
def process_large_file(filename):
    data = open(filename).read()  # Can cause memory issues
    return data.split('\n')
```

**Good Example:**
```python
# Stream processing
def process_large_file(filename):
    with open(filename) as f:
        for line in f:  # Processes one line at a time
            yield line.strip()
```

### 4. Repeated Calculations

**Bad Example:**
```python
# Recalculating the same value
def process_data(items):
    for item in items:
        if item > len(items) / 2:  # len(items) calculated every iteration
            print(item)
```

**Good Example:**
```python
# Cache the result
def process_data(items):
    threshold = len(items) / 2  # Calculate once
    for item in items:
        if item > threshold:
            print(item)
```

### 5. String Concatenation in Loops

**Bad Example:**
```python
# Inefficient string concatenation
result = ""
for i in range(1000):
    result += str(i)  # Creates new string each time
```

**Good Example:**
```python
# Use join for better performance
result = "".join(str(i) for i in range(1000))
```

## Profiling Tools

### Python
- **cProfile**: Built-in profiler
  ```bash
  python -m cProfile -s cumulative script.py
  ```
- **line_profiler**: Line-by-line profiling
- **memory_profiler**: Memory usage profiling
- **py-spy**: Sampling profiler for production

### JavaScript/Node.js
- **Chrome DevTools**: Built-in profiler
- **Node.js --prof**: V8 profiler
- **clinic.js**: Performance profiling suite

### Java
- **JProfiler**: Commercial profiler
- **VisualVM**: Free profiling tool
- **YourKit**: Performance analysis

### General
- **time/timeit**: Simple timing
- **perf**: Linux performance analysis
- **Valgrind**: Memory profiling (C/C++)

## Optimization Techniques

### 1. Use Appropriate Data Structures

| Operation | Bad Choice | Good Choice | Time Complexity |
|-----------|------------|-------------|-----------------|
| Membership test | List | Set/Dict | O(1) vs O(n) |
| FIFO queue | List | deque | O(1) vs O(n) |
| Priority queue | Sorted list | heapq | O(log n) vs O(n) |

### 2. Lazy Evaluation

```python
# Eager (loads all at once)
results = [process_item(x) for x in huge_list]

# Lazy (processes on demand)
results = (process_item(x) for x in huge_list)
```

### 3. Caching/Memoization

```python
from functools import lru_cache

@lru_cache(maxsize=128)
def fibonacci(n):
    if n < 2:
        return n
    return fibonacci(n-1) + fibonacci(n-2)
```

### 4. Parallel Processing

```python
from concurrent.futures import ThreadPoolExecutor

# Sequential
results = [process(item) for item in items]

# Parallel
with ThreadPoolExecutor(max_workers=4) as executor:
    results = list(executor.map(process, items))
```

### 5. Database Optimization

- Use indexes on frequently queried columns
- Avoid SELECT * (specify needed columns)
- Use connection pooling
- Batch operations instead of individual queries
- Use query result caching

### 6. Avoid Premature Optimization

Focus on:
1. **Hotspots**: Profile to find actual bottlenecks
2. **Big O**: Improve algorithmic complexity first
3. **Measure**: Always benchmark before and after

## Best Practices

### 1. Profile First
Don't guess where the bottleneck is. Use profiling tools to identify actual slow code.

### 2. Optimize the Right Thing
- 80/20 rule: 80% of time is spent in 20% of code
- Focus on hot paths and frequently called functions
- Consider user experience (what feels slow?)

### 3. Consider Trade-offs
- **Time vs Space**: Caching uses memory to save time
- **Complexity vs Performance**: Simple code is easier to maintain
- **Optimization vs Readability**: Don't sacrifice clarity for micro-optimizations

### 4. Benchmark Properly
```python
import timeit

# Good benchmarking
def benchmark():
    setup = "from __main__ import my_function"
    stmt = "my_function(1000)"
    time = timeit.timeit(stmt, setup, number=1000)
    print(f"Average time: {time/1000:.6f} seconds")
```

### 5. Use Built-in Functions
Built-in functions are usually implemented in C and are faster:
```python
# Slower
sum_val = 0
for x in numbers:
    sum_val += x

# Faster
sum_val = sum(numbers)
```

## Language-Specific Tips

### Python
- Use list comprehensions instead of loops
- Use `collections` module (defaultdict, Counter, deque)
- Consider NumPy for numerical operations
- Use `__slots__` for classes with many instances
- Avoid global variables

### JavaScript
- Minimize DOM manipulation
- Use event delegation
- Debounce/throttle frequent events
- Use `const` and `let` instead of `var`
- Avoid memory leaks (remove event listeners)

### Java
- Use StringBuilder for string concatenation
- Choose correct collection types
- Avoid creating unnecessary objects
- Use primitive types when possible
- Consider using streams for parallel processing

### C++
- Use references to avoid copies
- Prefer `std::vector` over arrays
- Use move semantics
- Enable compiler optimizations (-O2, -O3)
- Use `constexpr` for compile-time constants

### SQL
- Create indexes on WHERE/JOIN columns
- Use EXPLAIN to analyze queries
- Avoid OR in WHERE (use UNION instead)
- Use appropriate data types
- Normalize/denormalize appropriately

## Common Antipatterns

### 1. Premature Optimization
> "Premature optimization is the root of all evil" - Donald Knuth

Write clear, correct code first. Optimize only when needed.

### 2. Micro-optimizations
Don't waste time on optimizations that save microseconds if the code runs once per day.

### 3. Ignoring Algorithmic Complexity
Optimizing O(n²) code with tricks is worse than switching to an O(n log n) algorithm.

### 4. Not Measuring
Always measure performance before and after optimization to verify improvement.

### 5. Optimizing Cold Paths
Optimize frequently executed code, not initialization code that runs once.

## Quick Checklist

- [ ] Profile to identify bottlenecks
- [ ] Check algorithm complexity (Big O)
- [ ] Use appropriate data structures
- [ ] Reduce database queries
- [ ] Cache expensive operations
- [ ] Use lazy evaluation when possible
- [ ] Consider parallel processing
- [ ] Minimize memory allocations
- [ ] Use built-in functions
- [ ] Measure improvements

## Resources

### Books
- "The Art of Computer Programming" by Donald Knuth
- "Programming Pearls" by Jon Bentley
- "High Performance Python" by Micha Gorelick

### Online
- Big-O Cheat Sheet: https://www.bigocheatsheet.com/
- Python Performance Tips: https://wiki.python.org/moin/PythonSpeed
- Web Performance: https://web.dev/performance/

### Tools
- Profilers for your language
- APM tools (New Relic, DataDog, etc.)
- Browser DevTools for web applications

## Conclusion

Remember the golden rule of optimization:
1. **Make it work** - Write correct code
2. **Make it right** - Write clean, maintainable code
3. **Make it fast** - Optimize only when necessary

Always profile before optimizing, and measure to verify improvements. Focus on algorithmic efficiency and actual bottlenecks rather than micro-optimizations.

Happy optimizing! 🚀

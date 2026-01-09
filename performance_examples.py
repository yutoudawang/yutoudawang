#!/usr/bin/env python3
"""
Performance Examples: Demonstrating slow vs. fast code patterns
Run this file to see timing comparisons between inefficient and efficient code.
"""

import time
import timeit
from functools import lru_cache
from collections import defaultdict, deque


def timing_decorator(func):
    """Decorator to measure function execution time"""
    def wrapper(*args, **kwargs):
        start = time.time()
        result = func(*args, **kwargs)
        end = time.time()
        print(f"{func.__name__}: {end - start:.6f} seconds")
        return result
    return wrapper


# Example 1: Algorithm Complexity - Duplicates Detection
print("=" * 60)
print("Example 1: Finding Duplicates - O(n²) vs O(n)")
print("=" * 60)

@timing_decorator
def has_duplicates_slow(items):
    """O(n²) approach - nested loops"""
    for i in range(len(items)):
        for j in range(i + 1, len(items)):
            if items[i] == items[j]:
                return True
    return False

@timing_decorator
def has_duplicates_fast(items):
    """O(n) approach - using set"""
    return len(items) != len(set(items))

# Test with larger dataset
test_data = list(range(5000)) + [4999]  # Duplicate at the end
print(f"Testing with {len(test_data)} items...")
has_duplicates_slow(test_data)
has_duplicates_fast(test_data)
print()


# Example 2: String Concatenation
print("=" * 60)
print("Example 2: String Concatenation - += vs join()")
print("=" * 60)

@timing_decorator
def concat_with_plus(n):
    """Inefficient: Creates new string each time"""
    result = ""
    for i in range(n):
        result += str(i)
    return result

@timing_decorator
def concat_with_join(n):
    """Efficient: Uses join"""
    return "".join(str(i) for i in range(n))

n = 10000
print(f"Concatenating {n} numbers...")
concat_with_plus(n)
concat_with_join(n)
print()


# Example 3: List Comprehension vs Loop
print("=" * 60)
print("Example 3: List Comprehension vs Traditional Loop")
print("=" * 60)

@timing_decorator
def square_with_loop(n):
    """Using traditional loop"""
    result = []
    for i in range(n):
        result.append(i * i)
    return result

@timing_decorator
def square_with_comprehension(n):
    """Using list comprehension"""
    return [i * i for i in range(n)]

n = 100000
print(f"Squaring {n} numbers...")
square_with_loop(n)
square_with_comprehension(n)
print()


# Example 4: Caching with Memoization
print("=" * 60)
print("Example 4: Fibonacci - Without vs With Caching")
print("=" * 60)

call_count_slow = 0
call_count_fast = 0

def fibonacci_slow(n):
    """Without memoization"""
    global call_count_slow
    call_count_slow += 1
    if n < 2:
        return n
    return fibonacci_slow(n - 1) + fibonacci_slow(n - 2)

@lru_cache(maxsize=None)
def fibonacci_fast(n):
    """With memoization"""
    global call_count_fast
    call_count_fast += 1
    if n < 2:
        return n
    return fibonacci_fast(n - 1) + fibonacci_fast(n - 2)

n = 30
print(f"Computing fibonacci({n})...")

start = time.time()
result_slow = fibonacci_slow(n)
time_slow = time.time() - start
print(f"fibonacci_slow: {time_slow:.6f} seconds, {call_count_slow} function calls")

call_count_fast = 0
start = time.time()
result_fast = fibonacci_fast(n)
time_fast = time.time() - start
print(f"fibonacci_fast: {time_fast:.6f} seconds, {call_count_fast} function calls")
print(f"Speedup: {time_slow / time_fast:.1f}x faster")
print()


# Example 5: Data Structure Choice
print("=" * 60)
print("Example 5: Membership Testing - List vs Set")
print("=" * 60)

@timing_decorator
def membership_list(items, searches):
    """Using list for membership test"""
    count = 0
    for search in searches:
        if search in items:
            count += 1
    return count

@timing_decorator
def membership_set(items, searches):
    """Using set for membership test"""
    items_set = set(items)
    count = 0
    for search in searches:
        if search in items_set:
            count += 1
    return count

items = list(range(10000))
searches = list(range(0, 10000, 10))
print(f"Testing membership of {len(searches)} items in collection of {len(items)}...")
membership_list(items, searches)
membership_set(items, searches)
print()


# Example 6: Avoiding Repeated Calculations
print("=" * 60)
print("Example 6: Avoiding Repeated Calculations")
print("=" * 60)

@timing_decorator
def process_with_recalc(items):
    """Recalculating length each iteration"""
    result = []
    for item in items:
        if item > len(items) / 2:  # len() called every iteration
            result.append(item)
    return result

@timing_decorator
def process_with_cache(items):
    """Cache the calculated value"""
    threshold = len(items) / 2  # Calculate once
    result = []
    for item in items:
        if item > threshold:
            result.append(item)
    return result

items = list(range(100000))
print(f"Processing {len(items)} items...")
process_with_recalc(items)
process_with_cache(items)
print()


# Example 7: Generator vs List
print("=" * 60)
print("Example 7: Memory Efficiency - Generator vs List")
print("=" * 60)

import sys

def list_squares(n):
    """Returns list of squares"""
    return [i * i for i in range(n)]

def gen_squares(n):
    """Generator for squares"""
    return (i * i for i in range(n))

n = 1000000
list_result = list_squares(n)
gen_result = gen_squares(n)

print(f"List memory: {sys.getsizeof(list_result):,} bytes")
print(f"Generator memory: {sys.getsizeof(gen_result):,} bytes")
print(f"Memory saved: {sys.getsizeof(list_result) / sys.getsizeof(gen_result):.1f}x less memory")
print()


# Example 8: Using Built-in Functions
print("=" * 60)
print("Example 8: Custom Loop vs Built-in Function")
print("=" * 60)

@timing_decorator
def sum_with_loop(numbers):
    """Manual summation"""
    total = 0
    for num in numbers:
        total += num
    return total

@timing_decorator
def sum_with_builtin(numbers):
    """Using built-in sum"""
    return sum(numbers)

numbers = list(range(1000000))
print(f"Summing {len(numbers)} numbers...")
sum_with_loop(numbers)
sum_with_builtin(numbers)
print()


# Summary
print("=" * 60)
print("SUMMARY: Key Performance Lessons")
print("=" * 60)
print("""
1. Algorithm Complexity Matters: O(n) vs O(n²) makes huge difference
2. Use Right Data Structures: Set for membership, deque for queues
3. String Concatenation: Use join() instead of +=
4. Cache Results: Memoization can provide massive speedups
5. Built-in Functions: They're optimized and faster than manual loops
6. Avoid Recalculation: Cache values that don't change in loops
7. Generators: Use for memory efficiency with large datasets
8. List Comprehensions: Generally faster than traditional loops

Remember: Profile first, optimize bottlenecks, measure improvements!
""")

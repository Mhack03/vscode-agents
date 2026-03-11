/**
 * Result<T, E> - Discriminated Union Pattern
 * Provides type-safe error handling without throwing exceptions
 */

type Ok<T> = { kind: 'ok'; value: T };
type Err<E> = { kind: 'error'; error: E };
type Result<T, E> = Ok<T> | Err<E>;

// Helper constructors
const ok = <T>(value: T): Result<T, never> => ({
  kind: 'ok',
  value,
});

const err = <E>(error: E): Result<never, E> => ({
  kind: 'error',
  error,
});

// Type guards
function isOk<T, E>(result: Result<T, E>): result is Ok<T> {
  return result.kind === 'ok';
}

function isErr<T, E>(result: Result<T, E>): result is Err<E> {
  return result.kind === 'error';
}

// Example domain error
interface ApiError {
  code: 'NOT_FOUND' | 'UNAUTHORIZED' | 'INVALID_INPUT' | 'SERVER_ERROR';
  message: string;
}

// Usage example
async function fetchUser(id: string): Promise<Result<{ id: string; name: string }, ApiError>> {
  if (!id) {
    return err({ code: 'INVALID_INPUT', message: 'ID cannot be empty' });
  }

  try {
    const response = await fetch(`/api/users/${id}`);

    if (response.status === 404) {
      return err({ code: 'NOT_FOUND', message: `User ${id} not found` });
    }

    if (response.status === 401) {
      return err({ code: 'UNAUTHORIZED', message: 'Not authenticated' });
    }

    if (!response.ok) {
      return err({ code: 'SERVER_ERROR', message: `HTTP ${response.status}` });
    }

    const data = await response.json();
    return ok(data);
  } catch (e) {
    return err({ code: 'SERVER_ERROR', message: String(e) });
  }
}

// Usage with exhaustive type narrowing
async function printUserInfo(userId: string) {
  const result = await fetchUser(userId);

  if (isOk(result)) {
    console.log(`User: ${result.value.name}`);
  } else {
    console.error(`Error [${result.error.code}]: ${result.error.message}`);
  }
}

// Chain multiple operations
async function getUserWithPosts(userId: string) {
  const userResult = await fetchUser(userId);
  if (!isOk(userResult)) {
    return userResult; // Early return with error
  }

  const postsResult = await fetchUserPosts(userResult.value.id);
  if (!isOk(postsResult)) {
    return postsResult;
  }

  return ok({
    user: userResult.value,
    posts: postsResult.value,
  });
}

// Placeholder
async function fetchUserPosts(userId: string): Promise<Result<any[], ApiError>> {
  return ok([]);
}

export { Result, Ok, Err, ok, err, isOk, isErr };

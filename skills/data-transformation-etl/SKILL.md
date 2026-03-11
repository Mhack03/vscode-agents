---
name: data-transformation-etl
description: Data transformation and ETL pipeline patterns for Python and TypeScript/Node.js. Use when building batch or streaming data pipelines, parsing CSV/JSON/XML files, validating schemas with Pydantic or Zod, cleaning and normalizing data, implementing incremental loading, chunked processing for large datasets, retry logic, idempotent operations, or monitoring pipeline performance and throughput.
license: Complete terms in LICENSE.txt
---

# Data Transformation & ETL

Production patterns for building reliable ETL pipelines in Python and TypeScript/Node.js — covering parsing, validation, transformation, loading, error handling, and performance optimization.

## When to Use This Skill

- Building data pipelines that extract from multiple sources
- Transforming raw data into structured, validated formats
- Integrating third-party data feeds (APIs, files, databases)
- Cleaning and normalizing messy or inconsistent data
- Implementing incremental data synchronization
- Processing large datasets with memory constraints
- Validating data schemas and enforcing data quality
- Handling errors, retries, and idempotency in data workflows
- Optimizing pipeline performance and throughput

## Prerequisites

```bash
# Python
pip install pandas pydantic

# TypeScript/Node.js
npm install zod csv-parse
```

## Key Concepts

| Concept               | Description                                        |
| --------------------- | -------------------------------------------------- |
| **Extract**           | Read data from sources (files, APIs, databases)    |
| **Transform**         | Clean, validate, normalize, and reshape data       |
| **Load**              | Write processed data to target systems             |
| **Watermark**         | Track last-synced position for incremental loads   |
| **Idempotency**       | Operations produce the same result if retried      |
| **Chunking**          | Process large datasets in memory-efficient batches |
| **Schema Validation** | Enforce data contracts with Pydantic/Zod           |
| **Checkpointing**     | Save pipeline state for resume after failures      |

## Data Parsing Quick Reference

### CSV Parsing (Python)

```python
import pandas as pd
from typing import Optional

def parse_csv_safely(
    file_path: str,
    expected_columns: list[str],
    date_columns: Optional[list[str]] = None
) -> pd.DataFrame:
    """Parse CSV with validation and error handling."""
    try:
        df = pd.read_csv(
            file_path,
            encoding='utf-8-sig',
            dtype=str,
            na_values=['', 'NULL', 'null', 'N/A'],
            parse_dates=date_columns or [],
            on_bad_lines='skip'
        )

        missing_cols = set(expected_columns) - set(df.columns)
        if missing_cols:
            raise ValueError(f"Missing required columns: {missing_cols}")

        str_cols = df.select_dtypes(include=['object']).columns
        df[str_cols] = df[str_cols].apply(lambda x: x.str.strip())
        return df[expected_columns]

    except pd.errors.EmptyDataError:
        return pd.DataFrame(columns=expected_columns)
```

### Schema Validation (Python — Pydantic)

```python
from pydantic import BaseModel, Field, field_validator
from datetime import datetime
from typing import Optional

class UserRecord(BaseModel):
    user_id: int = Field(gt=0)
    email: str
    name: str
    signup_date: datetime
    age: Optional[int] = Field(None, ge=0, le=150)

    @field_validator('email')
    @classmethod
    def validate_email(cls, v: str) -> str:
        if '@' not in v or '.' not in v.split('@')[-1]:
            raise ValueError(f"Invalid email: {v}")
        return v.lower().strip()
```

### Schema Validation (TypeScript — Zod)

```typescript
import { z } from "zod";

const UserSchema = z.object({
	userId: z.number().positive(),
	email: z.string().email(),
	name: z.string().min(1).max(200),
	signupDate: z.coerce.date(),
	age: z.number().int().min(0).max(150).optional(),
});

type User = z.infer<typeof UserSchema>;

function validateBatch(records: unknown[]): { valid: User[]; errors: any[] } {
	const valid: User[] = [];
	const errors: any[] = [];

	for (const [index, record] of records.entries()) {
		const result = UserSchema.safeParse(record);
		if (result.success) {
			valid.push(result.data);
		} else {
			errors.push({ index, issues: result.error.issues });
		}
	}
	return { valid, errors };
}
```

## Transformation Patterns

### Data Cleaning (Python)

```python
import pandas as pd

def clean_dataframe(df: pd.DataFrame) -> pd.DataFrame:
    """Standard cleaning pipeline."""
    return (
        df
        .drop_duplicates()
        .pipe(lambda d: d.assign(
            email=d['email'].str.lower().str.strip(),
            phone=d['phone'].str.replace(r'[^\d+]', '', regex=True),
            name=d['name'].str.title()
        ))
        .dropna(subset=['email'])
    )
```

### Type-Safe Transformation (TypeScript)

```typescript
interface RawRecord {
	amount: string;
	date: string;
	status: string;
}

interface CleanRecord {
	amount: number;
	date: Date;
	status: "active" | "inactive";
	processedAt: Date;
}

function transformRecord(raw: RawRecord): CleanRecord {
	return {
		amount: parseFloat(raw.amount),
		date: new Date(raw.date),
		status: raw.status.toLowerCase() === "active" ? "active" : "inactive",
		processedAt: new Date(),
	};
}
```

### Stream Processing (TypeScript)

```typescript
import { Transform } from "stream";

class RecordTransform extends Transform {
	constructor(private transformFn: (record: any) => any) {
		super({ objectMode: true });
	}

	_transform(chunk: any, _encoding: string, callback: Function) {
		try {
			const result = this.transformFn(chunk);
			this.push(result);
			callback();
		} catch (error) {
			callback(error);
		}
	}
}
```

## ETL Pipeline Design

### Basic Pipeline Structure (Python)

```python
from dataclasses import dataclass
from datetime import datetime
from typing import Optional, Callable
from pathlib import Path

@dataclass
class PipelineRun:
    run_id: str
    start_time: datetime
    end_time: Optional[datetime] = None
    status: str = 'running'
    records_processed: int = 0
    records_failed: int = 0

class ETLPipeline:
    def __init__(self, name: str, checkpoint_dir: Path):
        self.name = name
        self.checkpoint_dir = checkpoint_dir
        self.checkpoint_dir.mkdir(parents=True, exist_ok=True)

    def execute(
        self,
        extract_fn: Callable,
        transform_fn: Callable,
        load_fn: Callable,
    ) -> PipelineRun:
        run = PipelineRun(
            run_id=f"{self.name}_{datetime.now().isoformat()}",
            start_time=datetime.now()
        )
        try:
            data = extract_fn()
            transformed, errors = transform_fn(data)
            run.records_processed = len(transformed)
            run.records_failed = len(errors)
            load_fn(transformed)
            run.status = 'completed'
        except Exception as e:
            run.status = 'failed'
            raise
        finally:
            run.end_time = datetime.now()
        return run
```

### Incremental Loading

```python
class IncrementalLoader:
    """Load only new/changed data using watermarks."""

    def load_incremental(self, source, fetch_fn, load_fn):
        last_sync = self.get_watermark(source)
        new_records = fetch_fn(last_sync)
        if not new_records:
            return
        load_fn(new_records)
        max_ts = max(r['updated_at'] for r in new_records)
        self.update_watermark(source, max_ts, count=len(new_records))
```

### Retry with Exponential Backoff (TypeScript)

```typescript
async function executeWithRetry<T>(
	operation: () => Promise<T>,
	maxRetries = 3,
	initialDelayMs = 1000
): Promise<T> {
	for (let attempt = 0; attempt <= maxRetries; attempt++) {
		try {
			return await operation();
		} catch (error: any) {
			if (attempt === maxRetries || !isRetryable(error)) throw error;
			const delay = initialDelayMs * Math.pow(2, attempt);
			await new Promise((r) => setTimeout(r, delay));
		}
	}
	throw new Error("Exhausted retries");
}

function isRetryable(error: any): boolean {
	return /ETIMEDOUT|ECONNREFUSED|429|50[234]/.test(String(error));
}
```

## Performance Optimization

### Chunked Processing (Python)

```python
import pandas as pd
from typing import Callable

def process_csv_chunks(
    file_path: str,
    transform_fn: Callable[[pd.DataFrame], pd.DataFrame],
    output_path: str,
    chunk_size: int = 10000
):
    """Process large CSVs in memory-efficient chunks."""
    first = True
    for chunk in pd.read_csv(file_path, chunksize=chunk_size):
        transformed = transform_fn(chunk)
        transformed.to_csv(
            output_path,
            mode='w' if first else 'a',
            header=first,
            index=False
        )
        first = False
```

### Idempotent Loading

```python
import hashlib, json
from typing import Dict, Any

def generate_record_hash(record: Dict[str, Any]) -> str:
    normalized = json.dumps(record, sort_keys=True)
    return hashlib.sha256(normalized.encode()).hexdigest()

def deduplicate(records: list[dict], existing_hashes: set[str]):
    new, dupes = [], []
    for r in records:
        h = generate_record_hash(r)
        (dupes if h in existing_hashes else new).append(r)
        existing_hashes.add(h)
    return new, dupes
```

## Troubleshooting

| Problem                       | Cause                         | Solution                                   |
| ----------------------------- | ----------------------------- | ------------------------------------------ |
| `MemoryError` on large files  | Loading entire file to RAM    | Use chunked processing or streaming        |
| Schema validation failures    | Upstream format changed       | Implement schema drift detection           |
| Duplicate records after retry | Non-idempotent loads          | Use record hashing / upserts               |
| Pipeline hangs                | Unhandled exception in stream | Add error handlers at each stage           |
| Slow throughput               | Sequential processing         | Add parallel workers or async I/O          |
| Data type mismatches          | Implicit casting              | Read all as strings first, cast explicitly |

## Common Pitfalls

- **Reading entire large files into memory** — always stream or chunk
- **No schema validation** — use Pydantic (Python) or Zod (TypeScript)
- **Ignoring encoding** — always specify `utf-8-sig` to handle BOM
- **Missing retry/backoff** — network sources will fail; build in retries
- **Mutable state in transforms** — use pure functions to avoid side effects
- **No monitoring** — track records in/out, duration, and error counts per stage

## Checklist

- [ ] Data sources identified and access configured
- [ ] Schema validation implemented (Pydantic/Zod)
- [ ] Error handling with logging at each pipeline stage
- [ ] Incremental loading with watermarks (if applicable)
- [ ] Chunked processing for large datasets
- [ ] Idempotent loads (deduplication or upserts)
- [ ] Retry logic with exponential backoff
- [ ] Pipeline monitoring and metrics
- [ ] Unit tests for transform functions
- [ ] Documentation for data flow and schema

## References

- [Data Parsing & Validation Patterns](references/data-parsing-validation.md) — CSV/JSON/XML parsing, Pydantic models, Zod schemas, data normalization
- [Transformation Patterns](references/transformation-patterns.md) — Filtering, mapping, cleaning, type-safe transforms, stream processing
- [ETL Pipeline Design](references/etl-pipeline-design.md) — Batch pipelines, incremental loading, retry logic, idempotency, chunked/parallel processing, monitoring

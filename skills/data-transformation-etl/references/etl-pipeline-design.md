# ETL Pipeline Design

Detailed patterns for batch processing, incremental loading, error handling, idempotency, and performance optimization.

## Batch Processing Pipeline (Python)

```python
from dataclasses import dataclass
from datetime import datetime
from typing import Optional, Callable, Any
import logging
from pathlib import Path

@dataclass
class PipelineRun:
    """Track pipeline execution metadata."""
    run_id: str
    start_time: datetime
    end_time: Optional[datetime] = None
    status: str = 'running'
    records_processed: int = 0
    records_failed: int = 0
    error_message: Optional[str] = None

class ETLPipeline:
    """Production-grade ETL pipeline with checkpointing."""

    def __init__(self, name: str, checkpoint_dir: Path):
        self.name = name
        self.checkpoint_dir = checkpoint_dir
        self.checkpoint_dir.mkdir(parents=True, exist_ok=True)

    def execute(
        self,
        extract_fn: Callable,
        transform_fn: Callable,
        load_fn: Callable,
        run_id: Optional[str] = None
    ) -> PipelineRun:
        """Execute ETL pipeline with error handling and checkpointing."""
        run_id = run_id or f"{self.name}_{datetime.now().isoformat()}"
        run = PipelineRun(run_id=run_id, start_time=datetime.now())

        try:
            checkpoint = self._load_checkpoint(run_id)
            if checkpoint:
                logging.info(f"Resuming from checkpoint: {checkpoint}")
                data = checkpoint['data']
            else:
                logging.info(f"Extracting data for {run_id}")
                data = extract_fn()
                self._save_checkpoint(run_id, 'extracted', data)

            logging.info(f"Transforming data for {run_id}")
            transformed, errors = transform_fn(data)
            run.records_processed = len(transformed)
            run.records_failed = len(errors)

            if errors:
                self._log_errors(run_id, errors)

            self._save_checkpoint(run_id, 'transformed', transformed)

            logging.info(f"Loading data for {run_id}")
            load_fn(transformed)

            run.status = 'completed'
            run.end_time = datetime.now()
            self._cleanup_checkpoint(run_id)

        except Exception as e:
            run.status = 'failed'
            run.error_message = str(e)
            run.end_time = datetime.now()
            logging.error(f"Pipeline failed: {e}", exc_info=True)
            raise
        finally:
            self._save_run_metadata(run)

        return run
```

## Incremental Loading Strategy

```python
from datetime import datetime
from typing import Optional, Callable
import sqlite3

class IncrementalLoader:
    """Load only new/changed data using watermarks."""

    def __init__(self, state_db: str):
        self.state_db = state_db
        self._init_state_table()

    def _init_state_table(self):
        with sqlite3.connect(self.state_db) as conn:
            conn.execute("""
                CREATE TABLE IF NOT EXISTS watermarks (
                    source TEXT PRIMARY KEY,
                    last_synced_at TIMESTAMP,
                    last_record_id TEXT,
                    records_processed INTEGER
                )
            """)

    def load_incremental(
        self,
        source: str,
        fetch_fn: Callable[[Optional[datetime]], list],
        load_fn: Callable[[list], None]
    ):
        """Load only records since last watermark."""
        last_sync = self.get_watermark(source)
        new_records = fetch_fn(last_sync)

        if not new_records:
            logging.info(f"No new records for {source}")
            return

        load_fn(new_records)
        max_timestamp = max(r['updated_at'] for r in new_records)
        self.update_watermark(source, max_timestamp, count=len(new_records))
        logging.info(f"Loaded {len(new_records)} new records for {source}")
```

## Error Handling & Retry Logic (TypeScript)

```typescript
interface RetryConfig {
	maxRetries: number;
	initialDelayMs: number;
	maxDelayMs: number;
	backoffMultiplier: number;
}

class RetryableETL {
	private defaultConfig: RetryConfig = {
		maxRetries: 3,
		initialDelayMs: 1000,
		maxDelayMs: 30000,
		backoffMultiplier: 2,
	};

	async executeWithRetry<T>(
		operation: () => Promise<T>,
		operationName: string,
		config: Partial<RetryConfig> = {}
	): Promise<T> {
		const cfg = { ...this.defaultConfig, ...config };
		let lastError: Error | undefined;

		for (let attempt = 0; attempt <= cfg.maxRetries; attempt++) {
			try {
				return await operation();
			} catch (error) {
				lastError = error as Error;
				if (attempt === cfg.maxRetries || !this.isRetryable(error)) throw error;

				const delay = Math.min(
					cfg.initialDelayMs * Math.pow(cfg.backoffMultiplier, attempt),
					cfg.maxDelayMs
				);
				await new Promise((resolve) => setTimeout(resolve, delay));
			}
		}
		throw lastError!;
	}

	private isRetryable(error: any): boolean {
		const retryablePatterns = [
			/ETIMEDOUT/,
			/ECONNREFUSED/,
			/429/,
			/502/,
			/503/,
			/504/,
		];
		return retryablePatterns.some((pattern) => pattern.test(error.toString()));
	}
}
```

## Idempotency in ETL (Python)

```python
import hashlib
import json
from typing import Any, Dict

class IdempotentLoader:
    """Ensure ETL operations can be safely retried."""

    @staticmethod
    def generate_record_hash(record: Dict[str, Any]) -> str:
        normalized = json.dumps(record, sort_keys=True)
        return hashlib.sha256(normalized.encode()).hexdigest()

    def load_with_deduplication(
        self,
        records: list[Dict],
        existing_hashes: set[str]
    ) -> tuple[list[Dict], list[Dict]]:
        new_records = []
        duplicates = []
        for record in records:
            record_hash = self.generate_record_hash(record)
            if record_hash in existing_hashes:
                duplicates.append(record)
            else:
                new_records.append(record)
                existing_hashes.add(record_hash)
        return new_records, duplicates
```

## Memory-Efficient Chunked Processing (Python)

```python
import pandas as pd
from typing import Callable

class ChunkedProcessor:
    """Process large files in memory-efficient chunks."""

    def __init__(self, chunk_size: int = 10000):
        self.chunk_size = chunk_size

    def process_csv_chunks(
        self,
        file_path: str,
        transform_fn: Callable[[pd.DataFrame], pd.DataFrame],
        output_path: str
    ):
        first_chunk = True
        total_processed = 0

        for chunk in pd.read_csv(file_path, chunksize=self.chunk_size):
            transformed = transform_fn(chunk)
            mode = 'w' if first_chunk else 'a'
            header = first_chunk
            transformed.to_csv(output_path, mode=mode, header=header, index=False)
            total_processed += len(transformed)
            first_chunk = False
```

## Parallel Processing (Python)

```python
from multiprocessing import Pool, cpu_count
import pandas as pd
from typing import List, Callable

class ParallelProcessor:
    def __init__(self, n_workers: int = None):
        self.n_workers = n_workers or cpu_count()

    def process_dataframe_parallel(
        self,
        df: pd.DataFrame,
        transform_fn: Callable[[pd.DataFrame], pd.DataFrame],
    ) -> pd.DataFrame:
        chunk_size = len(df) // self.n_workers
        chunks = [df.iloc[i:i + chunk_size] for i in range(0, len(df), chunk_size)]
        with Pool(self.n_workers) as pool:
            results = pool.map(transform_fn, chunks)
        return pd.concat(results, ignore_index=True)
```

## Pipeline Monitoring (Python)

```python
import time
from contextlib import contextmanager
from dataclasses import dataclass
from typing import Optional
import logging

@dataclass
class PipelineMetrics:
    stage: str
    start_time: float
    end_time: Optional[float] = None
    records_in: int = 0
    records_out: int = 0
    errors: int = 0

    @property
    def duration_seconds(self) -> float:
        return (self.end_time or time.time()) - self.start_time

    @property
    def throughput(self) -> float:
        duration = self.duration_seconds
        return self.records_out / duration if duration > 0 else 0

class MonitoredPipeline:
    def __init__(self, name: str):
        self.name = name
        self.metrics: list[PipelineMetrics] = []

    @contextmanager
    def monitor_stage(self, stage_name: str, records_in: int = 0):
        metrics = PipelineMetrics(stage=stage_name, start_time=time.time(), records_in=records_in)
        try:
            yield metrics
        except Exception as e:
            metrics.errors += 1
            raise
        finally:
            metrics.end_time = time.time()
            self.metrics.append(metrics)
            logging.info(
                f"Stage: {stage_name} | Duration: {metrics.duration_seconds:.2f}s | "
                f"Throughput: {metrics.throughput:.0f} rec/s | Errors: {metrics.errors}"
            )
```

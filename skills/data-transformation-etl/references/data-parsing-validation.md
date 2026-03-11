# Data Parsing & Validation

Detailed patterns for parsing CSV, JSON, XML data with validation in Python and TypeScript.

## CSV/Excel Parsing with Pandas (Python)

```python
# ✅ Good - Robust CSV parsing with validation
import pandas as pd
from typing import Optional
import logging

def parse_csv_safely(
    file_path: str,
    expected_columns: list[str],
    date_columns: Optional[list[str]] = None
) -> pd.DataFrame:
    """
    Parse CSV with validation and error handling.
    """
    try:
        df = pd.read_csv(
            file_path,
            encoding='utf-8-sig',  # Handle BOM
            dtype=str,  # Read everything as string initially
            na_values=['', 'NULL', 'null', 'N/A'],
            parse_dates=date_columns or [],
            date_format='ISO8601',
            on_bad_lines='skip'  # Skip malformed rows
        )

        # Validate expected columns
        missing_cols = set(expected_columns) - set(df.columns)
        if missing_cols:
            raise ValueError(f"Missing required columns: {missing_cols}")

        # Strip whitespace from string columns
        str_cols = df.select_dtypes(include=['object']).columns
        df[str_cols] = df[str_cols].apply(lambda x: x.str.strip())

        logging.info(f"Parsed {len(df)} rows from {file_path}")
        return df[expected_columns]  # Return only expected columns

    except pd.errors.EmptyDataError:
        logging.warning(f"Empty file: {file_path}")
        return pd.DataFrame(columns=expected_columns)
    except Exception as e:
        logging.error(f"Failed to parse {file_path}: {e}")
        raise

# Usage
df = parse_csv_safely(
    'data/sales.csv',
    expected_columns=['order_id', 'customer_id', 'amount', 'date'],
    date_columns=['date']
)
```

## JSON Parsing with Validation (Python)

```python
# ✅ Good - Type-safe JSON parsing with Pydantic
from pydantic import BaseModel, Field, field_validator, ValidationError
from datetime import datetime
from typing import Optional
import json

class UserRecord(BaseModel):
    """Validated user record schema."""
    user_id: int = Field(gt=0)
    email: str
    name: str
    signup_date: datetime
    age: Optional[int] = Field(None, ge=0, le=150)
    metadata: dict = Field(default_factory=dict)

    @field_validator('email')
    @classmethod
    def validate_email(cls, v: str) -> str:
        if '@' not in v or '.' not in v.split('@')[1]:
            raise ValueError('Invalid email format')
        return v.lower()

    @field_validator('name')
    @classmethod
    def validate_name(cls, v: str) -> str:
        cleaned = v.strip()
        if len(cleaned) < 2:
            raise ValueError('Name too short')
        return cleaned

def parse_json_records(json_data: str) -> list[UserRecord]:
    """Parse and validate JSON records."""
    try:
        raw_data = json.loads(json_data)
        records = []
        errors = []

        for idx, item in enumerate(raw_data):
            try:
                record = UserRecord(**item)
                records.append(record)
            except ValidationError as e:
                errors.append(f"Row {idx}: {e}")

        if errors and len(errors) > len(raw_data) * 0.5:
            raise ValueError(f"Too many validation errors: {len(errors)}")

        if errors:
            logging.warning(f"Skipped {len(errors)} invalid records")

        return records
    except json.JSONDecodeError as e:
        raise ValueError(f"Invalid JSON: {e}")
```

## CSV Parsing in Node.js/TypeScript

```typescript
// ✅ Good - Streaming CSV parser with validation
import { createReadStream } from "fs";
import { parse } from "csv-parse";
import { z } from "zod";

const SalesRecordSchema = z.object({
	order_id: z.string().min(1),
	customer_id: z.string().uuid(),
	amount: z.coerce.number().positive(),
	date: z.coerce.date(),
	status: z.enum(["pending", "completed", "cancelled"]),
});

type SalesRecord = z.infer<typeof SalesRecordSchema>;

async function parseCsvStream(
	filePath: string
): Promise<{
	valid: SalesRecord[];
	errors: Array<{ row: number; error: string }>;
}> {
	const valid: SalesRecord[] = [];
	const errors: Array<{ row: number; error: string }> = [];
	let rowNum = 0;

	const parser = createReadStream(filePath).pipe(
		parse({
			columns: true,
			skip_empty_lines: true,
			trim: true,
			cast: false,
		})
	);

	for await (const record of parser) {
		rowNum++;
		try {
			const validated = SalesRecordSchema.parse(record);
			valid.push(validated);
		} catch (error) {
			if (error instanceof z.ZodError) {
				errors.push({
					row: rowNum,
					error: error.errors.map((e) => `${e.path}: ${e.message}`).join(", "),
				});
			}
		}
	}

	console.log(`Parsed ${valid.length} valid records, ${errors.length} errors`);
	return { valid, errors };
}
```

## XML Parsing (Node.js)

```typescript
// ✅ Good - XML parser with validation
import { XMLParser } from "fast-xml-parser";
import { z } from "zod";

const ProductSchema = z.object({
	id: z.string(),
	name: z.string().min(1),
	price: z.number().positive(),
	inventory: z.object({
		quantity: z.number().int().nonnegative(),
		warehouse: z.string(),
	}),
});

type Product = z.infer<typeof ProductSchema>;

async function parseXmlFeed(xmlPath: string): Promise<Product[]> {
	const parser = new XMLParser({
		ignoreAttributes: false,
		attributeNamePrefix: "@_",
		parseAttributeValue: true,
		trimValues: true,
	});

	const xmlContent = await fs.promises.readFile(xmlPath, "utf-8");
	const parsed = parser.parse(xmlContent);

	const products: Product[] = [];
	const items = parsed.catalog?.product || [];
	const itemsArray = Array.isArray(items) ? items : [items];

	for (const item of itemsArray) {
		try {
			const validated = ProductSchema.parse(item);
			products.push(validated);
		} catch (error) {
			console.warn(`Skipping invalid product: ${item.id}`, error);
		}
	}

	return products;
}
```

## Data Type Coercion & Normalization

```python
# ✅ Good - Safe type coercion with fallbacks
import pandas as pd
import numpy as np

class DataNormalizer:
    """Safely normalize and coerce data types."""

    @staticmethod
    def normalize_numeric(series: pd.Series, default=0.0) -> pd.Series:
        """Convert to numeric with safe fallback."""
        if series.dtype == object:
            series = series.str.replace(r'[$,]', '', regex=True)
        numeric = pd.to_numeric(series, errors='coerce')
        return numeric.fillna(default)

    @staticmethod
    def normalize_dates(series: pd.Series) -> pd.Series:
        """Parse dates flexibly with multiple formats."""
        formats = ['%Y-%m-%d', '%m/%d/%Y', '%d-%m-%Y', '%Y%m%d']
        result = pd.Series(index=series.index, dtype='datetime64[ns]')

        for fmt in formats:
            mask = result.isna()
            result[mask] = pd.to_datetime(series[mask], format=fmt, errors='coerce')

        return result

    @staticmethod
    def normalize_strings(
        series: pd.Series,
        lowercase: bool = True,
        strip: bool = True
    ) -> pd.Series:
        """Normalize string values."""
        result = series.fillna('').astype(str)
        if strip:
            result = result.str.strip()
        if lowercase:
            result = result.str.lower()
        result = result.str.replace(r'\s+', ' ', regex=True)
        return result
```

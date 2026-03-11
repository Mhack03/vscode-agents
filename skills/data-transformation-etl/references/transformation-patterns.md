# Transformation Patterns

Detailed data transformation patterns including filtering, mapping, cleaning, and stream processing.

## Filtering & Mapping Operations (Python)

```python
from dataclasses import dataclass
from typing import List
from datetime import datetime
import pandas as pd

@dataclass
class OrderRecord:
    order_id: str
    customer_id: str
    amount: float
    status: str
    created_at: datetime

class OrderTransformer:
    """Type-safe order data transformations."""

    @staticmethod
    def filter_valid_orders(orders: List[OrderRecord]) -> List[OrderRecord]:
        """Filter for valid, completed orders."""
        return [
            order for order in orders
            if order.amount > 0
            and order.status == 'completed'
            and order.customer_id.strip()
        ]

    @staticmethod
    def enrich_with_revenue_tier(orders: List[OrderRecord]) -> List[dict]:
        """Add revenue tier classification."""
        def get_tier(amount: float) -> str:
            if amount >= 1000: return 'premium'
            elif amount >= 100: return 'standard'
            return 'basic'

        return [
            {
                **order.__dict__,
                'revenue_tier': get_tier(order.amount),
                'is_high_value': order.amount >= 500
            }
            for order in orders
        ]

    @staticmethod
    def aggregate_by_customer(df: pd.DataFrame) -> pd.DataFrame:
        """Aggregate orders by customer with metrics."""
        return df.groupby('customer_id').agg({
            'order_id': 'count',
            'amount': ['sum', 'mean', 'max'],
            'created_at': ['min', 'max']
        })
```

## Data Cleaning (Python)

```python
import pandas as pd
import numpy as np
from typing import Optional
import logging

class DataCleaner:
    """Production-grade data cleaning utilities."""

    @staticmethod
    def remove_duplicates(
        df: pd.DataFrame,
        subset: Optional[list] = None,
        keep: str = 'first'
    ) -> pd.DataFrame:
        """Remove duplicates with logging."""
        initial_count = len(df)
        df_clean = df.drop_duplicates(subset=subset, keep=keep)
        removed = initial_count - len(df_clean)
        if removed > 0:
            logging.info(f"Removed {removed} duplicate rows ({removed/initial_count*100:.1f}%)")
        return df_clean

    @staticmethod
    def handle_outliers(
        df: pd.DataFrame,
        column: str,
        method: str = 'iqr',
        threshold: float = 1.5
    ) -> pd.DataFrame:
        """Remove or cap outliers using IQR or z-score."""
        if method == 'iqr':
            Q1 = df[column].quantile(0.25)
            Q3 = df[column].quantile(0.75)
            IQR = Q3 - Q1
            lower_bound = Q1 - threshold * IQR
            upper_bound = Q3 + threshold * IQR
            df[column] = df[column].clip(lower=lower_bound, upper=upper_bound)
        elif method == 'zscore':
            z_scores = np.abs((df[column] - df[column].mean()) / df[column].std())
            df = df[z_scores < threshold]
        return df

    @staticmethod
    def handle_missing_values(
        df: pd.DataFrame,
        strategy: dict[str, str]
    ) -> pd.DataFrame:
        """
        Handle missing values with column-specific strategies.

        strategy = {
            'age': 'median',
            'name': 'drop',
            'country': 'mode',
            'description': 'constant:Unknown'
        }
        """
        df_clean = df.copy()
        for column, method in strategy.items():
            if column not in df_clean.columns:
                continue
            if method == 'drop':
                df_clean = df_clean.dropna(subset=[column])
            elif method == 'mean':
                df_clean[column].fillna(df_clean[column].mean(), inplace=True)
            elif method == 'median':
                df_clean[column].fillna(df_clean[column].median(), inplace=True)
            elif method == 'mode':
                df_clean[column].fillna(df_clean[column].mode()[0], inplace=True)
            elif method.startswith('constant:'):
                value = method.split(':', 1)[1]
                df_clean[column].fillna(value, inplace=True)
        return df_clean
```

## Type-Safe Transformations (TypeScript)

```typescript
import { z } from "zod";

interface RawOrderData {
	orderId: string;
	customerId: string;
	items: string; // JSON string
	total: string; // String number
	date: string;
}

interface ProcessedOrder {
	orderId: string;
	customerId: string;
	items: OrderItem[];
	total: number;
	date: Date;
	itemCount: number;
	averageItemPrice: number;
}

interface OrderItem {
	sku: string;
	quantity: number;
	price: number;
}

class OrderTransformPipeline {
	static transform(rawOrders: RawOrderData[]): ProcessedOrder[] {
		return rawOrders
			.map((order) => this.parseOrder(order))
			.filter((order): order is ProcessedOrder => order !== null)
			.map((order) => this.enrichOrder(order));
	}

	private static parseOrder(raw: RawOrderData): ProcessedOrder | null {
		try {
			const items: OrderItem[] = JSON.parse(raw.items);
			const itemSchema = z.array(
				z.object({
					sku: z.string(),
					quantity: z.number().int().positive(),
					price: z.number().positive(),
				})
			);
			const validatedItems = itemSchema.parse(items);

			return {
				orderId: raw.orderId,
				customerId: raw.customerId,
				items: validatedItems,
				total: parseFloat(raw.total),
				date: new Date(raw.date),
				itemCount: validatedItems.length,
				averageItemPrice: this.calculateAveragePrice(validatedItems),
			};
		} catch (error) {
			console.warn(`Failed to parse order ${raw.orderId}:`, error);
			return null;
		}
	}

	private static enrichOrder(order: ProcessedOrder): ProcessedOrder {
		return {
			...order,
			itemCount: order.items.length,
			averageItemPrice: this.calculateAveragePrice(order.items),
		};
	}

	private static calculateAveragePrice(items: OrderItem[]): number {
		if (items.length === 0) return 0;
		const totalPrice = items.reduce(
			(sum, item) => sum + item.price * item.quantity,
			0
		);
		const totalQuantity = items.reduce((sum, item) => sum + item.quantity, 0);
		return totalPrice / totalQuantity;
	}
}
```

## Stream Processing (Node.js)

```typescript
import { Transform, pipeline } from "stream";
import { createReadStream, createWriteStream } from "fs";
import { parse } from "csv-parse";
import { stringify } from "csv-stringify";
import { promisify } from "util";

const pipelineAsync = promisify(pipeline);

class DataTransformStream extends Transform {
	private rowCount = 0;
	private errorCount = 0;

	constructor(
		private validator: (row: any) => boolean,
		private transformer: (row: any) => any
	) {
		super({ objectMode: true });
	}

	_transform(chunk: any, encoding: string, callback: Function) {
		this.rowCount++;
		try {
			if (this.validator(chunk)) {
				const transformed = this.transformer(chunk);
				this.push(transformed);
			} else {
				this.errorCount++;
			}
		} catch (error) {
			this.errorCount++;
		}
		callback();
	}

	_flush(callback: Function) {
		console.log(`Processed ${this.rowCount} rows, ${this.errorCount} errors`);
		callback();
	}
}

async function processLargeDataset(inputPath: string, outputPath: string) {
	const validator = (row: any) => row.amount && parseFloat(row.amount) > 0;
	const transformer = (row: any) => ({
		...row,
		amount: parseFloat(row.amount),
		processed_at: new Date().toISOString(),
	});

	await pipelineAsync(
		createReadStream(inputPath),
		parse({ columns: true }),
		new DataTransformStream(validator, transformer),
		stringify({ header: true }),
		createWriteStream(outputPath)
	);
}
```

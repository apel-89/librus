WITH per_book AS (
    SELECT
        percentile_cont(0.5) WITHIN GROUP (
            ORDER BY f.reading_minutes::double precision
        ) AS median_minutes,
        count(*)::int AS sample_size
    FROM feedback f
    WHERE f.book_id = @book_id
      AND f.reading_minutes IS NOT NULL
      AND f.deleted_at IS NULL
),
global_rate AS (
    SELECT percentile_cont(0.5) WITHIN GROUP (
        ORDER BY f.reading_minutes::double precision / b.pages
    ) AS minutes_per_page
    FROM feedback f
    JOIN books b ON b.id = f.book_id
    WHERE f.reading_minutes IS NOT NULL
      AND f.deleted_at IS NULL
      AND b.pages > 0
)
SELECT
    per_book.median_minutes,
    per_book.sample_size,
    global_rate.minutes_per_page AS global_minutes_per_page
FROM per_book
CROSS JOIN global_rate
SELECT
    b.id AS book_id,
    count(l.id)::int AS loan_count
FROM loans l
JOIN book_copies c ON c.id = l.copy_id
JOIN books b ON b.id = c.book_id
WHERE (@since IS NULL OR l.borrowed_at >= @since)
GROUP BY b.id
ORDER BY loan_count DESC, b.id
LIMIT @limit
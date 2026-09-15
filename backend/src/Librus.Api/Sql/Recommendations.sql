WITH borrowers AS (
    SELECT DISTINCT l.user_id
    FROM loans l
    JOIN book_copies c ON c.id = l.copy_id
    WHERE c.book_id = @book_id
),
already_read AS (
    SELECT DISTINCT c.book_id
    FROM loans l
    JOIN book_copies c ON c.id = l.copy_id
    WHERE l.user_id = @user_id
)
SELECT
    c.book_id,
    count(DISTINCT l.user_id)::int AS loan_count
FROM loans l
JOIN book_copies c ON c.id = l.copy_id
JOIN borrowers b ON b.user_id = l.user_id
WHERE c.book_id <> @book_id
  AND c.book_id NOT IN (SELECT book_id FROM already_read)
GROUP BY c.book_id
ORDER BY loan_count DESC, c.book_id
LIMIT @limit
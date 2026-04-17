CREATE TABLE IF NOT EXISTS public.workout_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    is_template_based BOOLEAN NOT NULL DEFAULT FALSE,
    template_id TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_workout_plans_user_id
    ON public.workout_plans(user_id);

CREATE INDEX IF NOT EXISTS idx_workout_plans_dates
    ON public.workout_plans(start_date, end_date);

ALTER TABLE public.workout_plans ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own workout plans"
    ON public.workout_plans
    FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own workout plans"
    ON public.workout_plans
    FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own workout plans"
    ON public.workout_plans
    FOR UPDATE
    USING (auth.uid() = user_id);

CREATE POLICY "Users can delete own workout plans"
    ON public.workout_plans
    FOR DELETE
    USING (auth.uid() = user_id);

CREATE TABLE IF NOT EXISTS public.planned_workouts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id UUID NOT NULL REFERENCES public.workout_plans(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    sport TEXT NOT NULL,
    scheduled_date DATE NOT NULL,
    planned_distance_km DOUBLE PRECISION,
    planned_duration_minutes INTEGER,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    is_template_generated BOOLEAN NOT NULL DEFAULT FALSE,
    completed_activity_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_planned_workouts_plan_id
    ON public.planned_workouts(plan_id);

CREATE INDEX IF NOT EXISTS idx_planned_workouts_user_id
    ON public.planned_workouts(user_id);

CREATE INDEX IF NOT EXISTS idx_planned_workouts_schedule
    ON public.planned_workouts(scheduled_date);

ALTER TABLE public.planned_workouts ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own planned workouts"
    ON public.planned_workouts
    FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own planned workouts"
    ON public.planned_workouts
    FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own planned workouts"
    ON public.planned_workouts
    FOR UPDATE
    USING (auth.uid() = user_id);

CREATE POLICY "Users can delete own planned workouts"
    ON public.planned_workouts
    FOR DELETE
    USING (auth.uid() = user_id);

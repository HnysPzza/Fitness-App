-- Create user_activities table for storing workout/activity data
-- Run this in your Supabase SQL Editor

CREATE TABLE IF NOT EXISTS public.user_activities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    sport TEXT NOT NULL,
    distance_km DOUBLE PRECISION NOT NULL DEFAULT 0,
    duration_ticks BIGINT NOT NULL DEFAULT 0,
    avg_speed_kmh DOUBLE PRECISION,
    max_speed_kmh DOUBLE PRECISION,
    elevation_gain_m DOUBLE PRECISION,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    coordinates_json TEXT NOT NULL DEFAULT '{"points":[]}'
);

-- Create index on user_id for faster queries
CREATE INDEX IF NOT EXISTS idx_user_activities_user_id 
    ON public.user_activities(user_id);

-- Create index on created_at for date-range queries
CREATE INDEX IF NOT EXISTS idx_user_activities_created_at 
    ON public.user_activities(created_at DESC);

-- Enable Row Level Security
ALTER TABLE public.user_activities ENABLE ROW LEVEL SECURITY;

-- Policy: Users can only read their own activities
CREATE POLICY "Users can view own activities" 
    ON public.user_activities
    FOR SELECT
    USING (auth.uid() = user_id);

-- Policy: Users can only insert their own activities
CREATE POLICY "Users can insert own activities" 
    ON public.user_activities
    FOR INSERT
    WITH CHECK (auth.uid() = user_id);

-- Policy: Users can only update their own activities
CREATE POLICY "Users can update own activities" 
    ON public.user_activities
    FOR UPDATE
    USING (auth.uid() = user_id);

-- Policy: Users can only delete their own activities
CREATE POLICY "Users can delete own activities" 
    ON public.user_activities
    FOR DELETE
    USING (auth.uid() = user_id);

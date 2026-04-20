package com.oreoreooooooo.VRM;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.util.AttributeSet;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewParent;

public class ColorWheelView extends View {
    interface OnColorChangeListener {
        void onColorChanged(int color, boolean fromUser);
    }

    private final Paint bitmapPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final Paint ringPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final Paint thumbPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final Paint thumbStrokePaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final float[] hsv = new float[]{0.0f, 1.0f, 1.0f};

    private Bitmap wheelBitmap;
    private boolean wheelDirty = true;
    private float centerX;
    private float centerY;
    private float radius;
    private float hue = 0.0f;
    private float saturation = 1.0f;
    private float value = 1.0f;
    private boolean trackingTouch;
    private OnColorChangeListener listener;

    public ColorWheelView(Context context) {
        super(context);
        init();
    }

    public ColorWheelView(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    public ColorWheelView(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
        init();
    }

    private void init() {
        setLayerType(View.LAYER_TYPE_SOFTWARE, null);

        ringPaint.setStyle(Paint.Style.STROKE);
        ringPaint.setStrokeWidth(dp(1.0f));
        ringPaint.setColor(Color.argb(120, 255, 255, 255));

        thumbPaint.setStyle(Paint.Style.FILL);
        thumbPaint.setColor(Color.WHITE);

        thumbStrokePaint.setStyle(Paint.Style.STROKE);
        thumbStrokePaint.setStrokeWidth(dp(2.0f));
        thumbStrokePaint.setColor(Color.argb(220, 20, 20, 24));
    }

    void setOnColorChangeListener(OnColorChangeListener listener) {
        this.listener = listener;
    }

    void setColor(int color) {
        Color.colorToHSV(color, hsv);
        hue = hsv[0];
        saturation = hsv[1];
        setValueInternal(hsv[2], false);
        invalidate();
    }

    int getColor() {
        hsv[0] = hue;
        hsv[1] = saturation;
        hsv[2] = value;
        return Color.HSVToColor(hsv);
    }

    float getHue() {
        return hue;
    }

    float getSaturation() {
        return saturation;
    }

    float getValue() {
        return value;
    }

    void setValue(float value) {
        setValueInternal(value, true);
    }

    private void setValueInternal(float nextValue, boolean redraw) {
        float clampedValue = clamp(nextValue, 0.0f, 1.0f);
        if (Math.abs(value - clampedValue) < 0.001f) {
            return;
        }

        value = clampedValue;
        if (redraw) {
            invalidate();
        }
    }

    @Override
    protected void onSizeChanged(int width, int height, int oldWidth, int oldHeight) {
        super.onSizeChanged(width, height, oldWidth, oldHeight);
        wheelDirty = true;
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);

        int width = getWidth();
        int height = getHeight();
        if (width <= 0 || height <= 0) {
            return;
        }

        centerX = width * 0.5f;
        centerY = height * 0.5f;
        radius = Math.max(1.0f, Math.min(width, height) * 0.5f - dp(8.0f));

        ensureWheelBitmap(width, height);
        canvas.drawBitmap(wheelBitmap, 0.0f, 0.0f, bitmapPaint);
        canvas.drawCircle(centerX, centerY, radius, ringPaint);

        double angle = Math.toRadians(hue);
        float thumbX = centerX + (float) Math.cos(angle) * saturation * radius;
        float thumbY = centerY + (float) Math.sin(angle) * saturation * radius;
        float thumbRadius = dp(7.0f);
        thumbPaint.setColor(getColor());
        canvas.drawCircle(thumbX, thumbY, thumbRadius, thumbPaint);
        canvas.drawCircle(thumbX, thumbY, thumbRadius, thumbStrokePaint);
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        int action = event.getActionMasked();
        if (action == MotionEvent.ACTION_DOWN) {
            updateGeometry();
            if (!isInsideWheel(event.getX(), event.getY())) {
                trackingTouch = false;
                requestParentDisallowIntercept(false);
                return false;
            }

            trackingTouch = true;
            requestParentDisallowIntercept(true);
            updateFromTouch(event.getX(), event.getY());
            if (listener != null) {
                listener.onColorChanged(getColor(), true);
            }
            invalidate();
            return true;
        }

        if (action == MotionEvent.ACTION_MOVE) {
            if (!trackingTouch) {
                return false;
            }

            requestParentDisallowIntercept(true);
            updateFromTouch(event.getX(), event.getY());
            if (listener != null) {
                listener.onColorChanged(getColor(), true);
            }
            invalidate();
            return true;
        }

        if (action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_CANCEL) {
            trackingTouch = false;
            requestParentDisallowIntercept(false);
            return true;
        }

        return true;
    }

    private void updateFromTouch(float x, float y) {
        float dx = x - centerX;
        float dy = y - centerY;
        float distance = (float) Math.sqrt(dx * dx + dy * dy);
        saturation = radius <= 0.0f ? 0.0f : clamp(distance / radius, 0.0f, 1.0f);
        if (distance > 0.001f) {
            hue = (float) Math.toDegrees(Math.atan2(dy, dx));
            if (hue < 0.0f) {
                hue += 360.0f;
            }
        }
    }

    private void updateGeometry() {
        centerX = getWidth() * 0.5f;
        centerY = getHeight() * 0.5f;
        radius = Math.max(1.0f, Math.min(getWidth(), getHeight()) * 0.5f - dp(8.0f));
    }

    private boolean isInsideWheel(float x, float y) {
        float dx = x - centerX;
        float dy = y - centerY;
        return dx * dx + dy * dy <= radius * radius;
    }

    private void ensureWheelBitmap(int width, int height) {
        if (wheelBitmap == null || wheelBitmap.getWidth() != width || wheelBitmap.getHeight() != height) {
            wheelBitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888);
            wheelDirty = true;
        }

        if (!wheelDirty) {
            return;
        }

        int[] pixels = new int[width * height];
        float[] pixelHsv = new float[]{0.0f, 0.0f, 1.0f};
        for (int y = 0; y < height; y++) {
            float dy = y - centerY;
            for (int x = 0; x < width; x++) {
                float dx = x - centerX;
                float distance = (float) Math.sqrt(dx * dx + dy * dy);
                int index = y * width + x;
                if (distance > radius) {
                    pixels[index] = Color.TRANSPARENT;
                    continue;
                }

                float angle = (float) Math.toDegrees(Math.atan2(dy, dx));
                if (angle < 0.0f) {
                    angle += 360.0f;
                }

                pixelHsv[0] = angle;
                pixelHsv[1] = radius <= 0.0f ? 0.0f : clamp(distance / radius, 0.0f, 1.0f);
                pixelHsv[2] = 1.0f;
                pixels[index] = Color.HSVToColor(pixelHsv);
            }
        }

        wheelBitmap.setPixels(pixels, 0, width, 0, 0, width, height);
        wheelDirty = false;
    }

    private float dp(float value) {
        return value * getResources().getDisplayMetrics().density;
    }

    private void requestParentDisallowIntercept(boolean disallow) {
        ViewParent parent = getParent();
        if (parent != null) {
            parent.requestDisallowInterceptTouchEvent(disallow);
        }
    }

    private static float clamp(float value, float min, float max) {
        return Math.max(min, Math.min(max, value));
    }
}

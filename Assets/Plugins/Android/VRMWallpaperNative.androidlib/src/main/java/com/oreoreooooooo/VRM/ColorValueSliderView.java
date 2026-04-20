package com.oreoreooooooo.VRM;

import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.LinearGradient;
import android.graphics.Paint;
import android.graphics.RectF;
import android.graphics.Shader;
import android.util.AttributeSet;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewParent;

public class ColorValueSliderView extends View {
    interface OnValueChangeListener {
        void onValueChanged(float value, boolean fromUser);
    }

    private final Paint barPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final Paint borderPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final Paint thumbPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final Paint thumbStrokePaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final RectF barRect = new RectF();
    private final float[] hsv = new float[]{0.0f, 1.0f, 1.0f};

    private float hue = 0.0f;
    private float saturation = 1.0f;
    private float value = 1.0f;
    private OnValueChangeListener listener;

    public ColorValueSliderView(Context context) {
        super(context);
        init();
    }

    public ColorValueSliderView(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    public ColorValueSliderView(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
        init();
    }

    private void init() {
        borderPaint.setStyle(Paint.Style.STROKE);
        borderPaint.setStrokeWidth(dp(1.0f));
        borderPaint.setColor(Color.argb(120, 255, 255, 255));

        thumbPaint.setStyle(Paint.Style.FILL);
        thumbPaint.setColor(Color.WHITE);

        thumbStrokePaint.setStyle(Paint.Style.STROKE);
        thumbStrokePaint.setStrokeWidth(dp(2.0f));
        thumbStrokePaint.setColor(Color.argb(220, 20, 20, 24));
    }

    void setOnValueChangeListener(OnValueChangeListener listener) {
        this.listener = listener;
    }

    void setColor(int color) {
        Color.colorToHSV(color, hsv);
        setHueSaturationValue(hsv[0], hsv[1], hsv[2]);
    }

    void setHueSaturationValue(float hue, float saturation, float value) {
        this.hue = clamp(hue, 0.0f, 360.0f);
        this.saturation = clamp(saturation, 0.0f, 1.0f);
        this.value = clamp(value, 0.0f, 1.0f);
        invalidate();
    }

    float getValue() {
        return value;
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);

        float padding = dp(10.0f);
        float barHeight = dp(24.0f);
        float centerY = getHeight() * 0.5f;
        barRect.set(padding, centerY - barHeight * 0.5f, getWidth() - padding, centerY + barHeight * 0.5f);

        hsv[0] = hue;
        hsv[1] = saturation;
        hsv[2] = 1.0f;
        int fullColor = Color.HSVToColor(hsv);
        barPaint.setShader(new LinearGradient(
                barRect.left, 0.0f, barRect.right, 0.0f,
                Color.BLACK, fullColor, Shader.TileMode.CLAMP));

        float radius = barHeight * 0.5f;
        canvas.drawRoundRect(barRect, radius, radius, barPaint);
        canvas.drawRoundRect(barRect, radius, radius, borderPaint);

        float thumbX = barRect.left + clamp(value, 0.0f, 1.0f) * barRect.width();
        hsv[2] = value;
        thumbPaint.setColor(Color.HSVToColor(hsv));
        canvas.drawCircle(thumbX, centerY, dp(8.0f), thumbPaint);
        canvas.drawCircle(thumbX, centerY, dp(8.0f), thumbStrokePaint);
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        int action = event.getActionMasked();
        if (action == MotionEvent.ACTION_DOWN || action == MotionEvent.ACTION_MOVE) {
            requestParentDisallowIntercept(true);
            updateFromTouch(event.getX());
            if (listener != null) {
                listener.onValueChanged(value, true);
            }
            invalidate();
            return true;
        }

        if (action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_CANCEL) {
            requestParentDisallowIntercept(false);
            return true;
        }

        return true;
    }

    private void updateFromTouch(float x) {
        value = barRect.width() <= 0.0f
                ? 0.0f
                : clamp((x - barRect.left) / barRect.width(), 0.0f, 1.0f);
    }

    private void requestParentDisallowIntercept(boolean disallow) {
        ViewParent parent = getParent();
        if (parent != null) {
            parent.requestDisallowInterceptTouchEvent(disallow);
        }
    }

    private float dp(float value) {
        return value * getResources().getDisplayMetrics().density;
    }

    private static float clamp(float value, float min, float max) {
        return Math.max(min, Math.min(max, value));
    }
}

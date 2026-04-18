package com.oreoreooooooo.VRM;

import android.app.Activity;
import android.app.WallpaperInfo;
import android.app.WallpaperManager;
import android.content.ComponentName;
import android.content.Intent;
import android.content.res.Configuration;
import android.database.Cursor;
import android.graphics.Color;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;
import android.text.Editable;
import android.text.TextWatcher;
import android.util.Log;
import android.view.View;
import android.view.ViewGroup;
import android.widget.AdapterView;
import android.widget.EditText;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.SeekBar;
import android.widget.Spinner;
import android.widget.Switch;
import android.widget.TextView;
import android.widget.Toast;

import com.unity3d.player.UnityPlayer;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.util.Locale;

public class MainActivity extends Activity {
    private static final String TAG = "VRMMainActivity";
    private static final String STARTUP_LOG_NAME = "mainactivity_start.log";
    private static final boolean UNITY_ONLY_STARTUP_PROBE = false;
    private static MainActivity activeInstance;
    private static String lastModelInfo = "";
    private static String lastFpsInfo = "";

    private static final int REQUEST_PICK_VRM = 1001;
    private static final int REQUEST_PICK_IMAGE = 1002;

    private static final float DISTANCE_MIN = 1.0f;
    private static final float DISTANCE_MAX = 10.0f;
    private static final float HEIGHT_MIN = -3.0f;
    private static final float HEIGHT_MAX = 3.0f;
    private static final int ANGLE_MIN = -180;
    private static final int ANGLE_MAX = 180;
    private static final float OFFSET_MIN = -2.0f;
    private static final float OFFSET_MAX = 2.0f;
    private static final float SCALE_MIN = 0.2f;
    private static final float SCALE_MAX = 3.0f;
    private static final float RENDER_SCALE_MIN = 0.5f;
    private static final float RENDER_SCALE_MAX = 1.0f;
    private static final int[] TARGET_FPS_VALUES = new int[]{20, 30, 45, 60};

    private UnityPlayer unityPlayer;

    private View settingsOverlay;
    private View toggleButton;
    private View imageAdjustmentPanel;
    private View colorPreview;

    private TextView vrmPathText;
    private TextView backgroundModeText;
    private TextView distanceValueText;
    private TextView heightValueText;
    private TextView angleValueText;
    private TextView offsetXValueText;
    private TextView offsetYValueText;
    private TextView scaleValueText;
    private TextView renderScaleValueText;
    private TextView modelOffsetXValueText;
    private TextView modelOffsetYValueText;
    private TextView modelOffsetZValueText;
    private TextView modelScaleValueText;
    private TextView modelInfoText;
    private TextView fpsInfoText;

    private SeekBar distanceSeekBar;
    private SeekBar heightSeekBar;
    private SeekBar angleSeekBar;
    private SeekBar redSeekBar;
    private SeekBar greenSeekBar;
    private SeekBar blueSeekBar;
    private SeekBar offsetXSeekBar;
    private SeekBar offsetYSeekBar;
    private SeekBar scaleSeekBar;
    private SeekBar renderScaleSeekBar;
    private SeekBar modelOffsetXSeekBar;
    private SeekBar modelOffsetYSeekBar;
    private SeekBar modelOffsetZSeekBar;
    private SeekBar modelScaleSeekBar;

    private Spinner fitModeSpinner;
    private Spinner targetFpsSpinner;
    private Spinner logLevelSpinner;
    private EditText hexColorEdit;
    private Switch touchEnabledSwitch;
    private Switch physBoneEnabledSwitch;
    private Switch fpsOverlaySwitch;

    private boolean suppressUiCallbacks;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        try {
            appendStartupLog("onCreate: enter");
            ContextHolder.setContext(this);
            Log.d(TAG, "onCreate");
            super.onCreate(savedInstanceState);
            appendStartupLog("onCreate: after super");
            activeInstance = this;

            appendStartupLog("onCreate: before UnityPlayer");
            unityPlayer = new UnityPlayer(this);
            appendStartupLog("onCreate: after UnityPlayer");

            if (UNITY_ONLY_STARTUP_PROBE) {
                appendStartupLog("onCreate: unity-only probe mode");
                setContentView(unityPlayer);
                appendStartupLog("onCreate: after setContentView(unityPlayer)");
            } else {
                setContentView(R.layout.vrm_activity_main);
                appendStartupLog("onCreate: after setContentView");
                FrameLayout container = findViewById(R.id.vrm_unity_container);
                appendStartupLog("onCreate: after findViewById container=" + (container != null));
                container.addView(unityPlayer, new FrameLayout.LayoutParams(
                        ViewGroup.LayoutParams.MATCH_PARENT,
                        ViewGroup.LayoutParams.MATCH_PARENT));
                appendStartupLog("onCreate: after addView");
            }

            unityPlayer.requestFocus();
            appendStartupLog("onCreate: after requestFocus");

            if (!UNITY_ONLY_STARTUP_PROBE) {
                bindViews();
                appendStartupLog("onCreate: after bindViews");
                bindActions();
                appendStartupLog("onCreate: after bindActions");
                loadCurrentSettings();
                appendStartupLog("onCreate: after loadCurrentSettings");
                showSettings();
                appendStartupLog("onCreate: after showSettings");
            }
        } catch (Throwable throwable) {
            appendStartupLog("onCreate: exception", throwable);
            throw throwable;
        }
    }

    @Override
    protected void onResume() {
        try {
            super.onResume();
            appendStartupLog("onResume: enter");
            if (unityPlayer != null) {
                unityPlayer.resume();
                appendStartupLog("onResume: after unityPlayer.resume");
            }
        } catch (Throwable throwable) {
            appendStartupLog("onResume: exception", throwable);
            throw throwable;
        }
    }

    @Override
    protected void onPause() {
        appendStartupLog("onPause: enter");
        if (unityPlayer != null) {
            unityPlayer.pause();
            appendStartupLog("onPause: after unityPlayer.pause");
        }
        super.onPause();
    }

    @Override
    protected void onDestroy() {
        appendStartupLog("onDestroy: enter");
        if (activeInstance == this) {
            activeInstance = null;
        }
        if (unityPlayer != null) {
            unityPlayer.quit();
            appendStartupLog("onDestroy: after unityPlayer.quit");
            unityPlayer = null;
        }
        super.onDestroy();
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        if (unityPlayer != null) {
            unityPlayer.configurationChanged(newConfig);
        }
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (unityPlayer != null) {
            unityPlayer.windowFocusChanged(hasFocus);
        }
    }

    @Override
    public void onLowMemory() {
        super.onLowMemory();
        if (unityPlayer != null) {
            unityPlayer.lowMemory();
        }
    }

    @Override
    public void onBackPressed() {
        if (settingsOverlay != null && settingsOverlay.getVisibility() == View.VISIBLE) {
            hideSettings();
            return;
        }
        super.onBackPressed();
    }

    // ===== Settings panel toggle =====

    private void showSettings() {
        settingsOverlay.setVisibility(View.VISIBLE);
        toggleButton.setVisibility(View.GONE);
        refreshFileLabels();
        updateImageAdjustmentVisibility();
    }

    private void hideSettings() {
        settingsOverlay.setVisibility(View.GONE);
        toggleButton.setVisibility(View.VISIBLE);
    }

    private void appendStartupLog(String message) {
        try {
            File baseDir = getExternalFilesDir(null);
            if (baseDir == null) {
                baseDir = getFilesDir();
            }
            if (baseDir == null) {
                Log.w(TAG, "appendStartupLog skipped: no writable dir");
                return;
            }

            File logFile = new File(baseDir, STARTUP_LOG_NAME);
            File parent = logFile.getParentFile();
            if (parent != null && !parent.exists()) {
                parent.mkdirs();
            }

            try (FileOutputStream stream = new FileOutputStream(logFile, true)) {
                String line = String.format(Locale.US, "%1$tF %1$tT.%1$tL %2$s%n", System.currentTimeMillis(), message);
                stream.write(line.getBytes());
                stream.flush();
            }
        } catch (Exception exception) {
            Log.e(TAG, "appendStartupLog failed: " + message, exception);
        }
    }

    private void appendStartupLog(String message, Throwable throwable) {
        appendStartupLog(message + "\n" + stackTraceToString(throwable));
    }

    private static String stackTraceToString(Throwable throwable) {
        if (throwable == null) {
            return "";
        }

        StringWriter writer = new StringWriter();
        PrintWriter printWriter = new PrintWriter(writer);
        throwable.printStackTrace(printWriter);
        printWriter.flush();
        return writer.toString();
    }

    // ===== View binding =====

    private void bindViews() {
        settingsOverlay = findViewById(R.id.vrm_settings_overlay);
        toggleButton = findViewById(R.id.vrm_button_toggle_settings);

        vrmPathText = findViewById(R.id.vrm_text_vrm_path);
        backgroundModeText = findViewById(R.id.vrm_text_background_mode);
        distanceValueText = findViewById(R.id.vrm_text_distance_value);
        heightValueText = findViewById(R.id.vrm_text_height_value);
        angleValueText = findViewById(R.id.vrm_text_angle_value);
        offsetXValueText = findViewById(R.id.vrm_text_offset_x_value);
        offsetYValueText = findViewById(R.id.vrm_text_offset_y_value);
        scaleValueText = findViewById(R.id.vrm_text_scale_value);
        renderScaleValueText = findViewById(R.id.vrm_text_render_scale_value);
        modelOffsetXValueText = findViewById(R.id.vrm_text_model_offset_x_value);
        modelOffsetYValueText = findViewById(R.id.vrm_text_model_offset_y_value);
        modelOffsetZValueText = findViewById(R.id.vrm_text_model_offset_z_value);
        modelScaleValueText = findViewById(R.id.vrm_text_model_scale_value);
        modelInfoText = findViewById(R.id.vrm_text_model_info);
        fpsInfoText = findViewById(R.id.vrm_text_fps_info);

        colorPreview = findViewById(R.id.vrm_view_color_preview);
        imageAdjustmentPanel = findViewById(R.id.vrm_panel_image_adjustment);

        distanceSeekBar = findViewById(R.id.vrm_seek_distance);
        heightSeekBar = findViewById(R.id.vrm_seek_height);
        angleSeekBar = findViewById(R.id.vrm_seek_angle);
        redSeekBar = findViewById(R.id.vrm_seek_red);
        greenSeekBar = findViewById(R.id.vrm_seek_green);
        blueSeekBar = findViewById(R.id.vrm_seek_blue);
        offsetXSeekBar = findViewById(R.id.vrm_seek_offset_x);
        offsetYSeekBar = findViewById(R.id.vrm_seek_offset_y);
        scaleSeekBar = findViewById(R.id.vrm_seek_scale);
        renderScaleSeekBar = findViewById(R.id.vrm_seek_render_scale);
        modelOffsetXSeekBar = findViewById(R.id.vrm_seek_model_offset_x);
        modelOffsetYSeekBar = findViewById(R.id.vrm_seek_model_offset_y);
        modelOffsetZSeekBar = findViewById(R.id.vrm_seek_model_offset_z);
        modelScaleSeekBar = findViewById(R.id.vrm_seek_model_scale);
        fitModeSpinner = findViewById(R.id.vrm_spinner_fit_mode);
        targetFpsSpinner = findViewById(R.id.vrm_spinner_target_fps);
        logLevelSpinner = findViewById(R.id.vrm_spinner_log_level);
        hexColorEdit = findViewById(R.id.vrm_edit_hex_color);
        touchEnabledSwitch = findViewById(R.id.vrm_switch_touch_enabled);
        physBoneEnabledSwitch = findViewById(R.id.vrm_switch_physbone_enabled);
        fpsOverlaySwitch = findViewById(R.id.vrm_switch_fps_overlay);

        distanceSeekBar.setMax(Math.round((DISTANCE_MAX - DISTANCE_MIN) * 10.0f));
        heightSeekBar.setMax(Math.round((HEIGHT_MAX - HEIGHT_MIN) * 20.0f));
        angleSeekBar.setMax(ANGLE_MAX - ANGLE_MIN);
        redSeekBar.setMax(255);
        greenSeekBar.setMax(255);
        blueSeekBar.setMax(255);
        offsetXSeekBar.setMax(Math.round((OFFSET_MAX - OFFSET_MIN) * 100.0f));
        offsetYSeekBar.setMax(Math.round((OFFSET_MAX - OFFSET_MIN) * 100.0f));
        scaleSeekBar.setMax(Math.round((SCALE_MAX - SCALE_MIN) * 100.0f));
        renderScaleSeekBar.setMax(Math.round((RENDER_SCALE_MAX - RENDER_SCALE_MIN) * 100.0f));
        modelOffsetXSeekBar.setMax(Math.round((OFFSET_MAX - OFFSET_MIN) * 100.0f));
        modelOffsetYSeekBar.setMax(Math.round((OFFSET_MAX - OFFSET_MIN) * 100.0f));
        modelOffsetZSeekBar.setMax(Math.round((OFFSET_MAX - OFFSET_MIN) * 100.0f));
        modelScaleSeekBar.setMax(Math.round((SCALE_MAX - SCALE_MIN) * 100.0f));

        ArrayAdapter<CharSequence> fitModeAdapter = createWhiteSpinnerAdapter(R.array.vrm_background_fit_modes);
        fitModeSpinner.setAdapter(fitModeAdapter);

        ArrayAdapter<CharSequence> fpsAdapter = createWhiteSpinnerAdapter(R.array.vrm_target_fps_options);
        targetFpsSpinner.setAdapter(fpsAdapter);

        ArrayAdapter<CharSequence> logLevelAdapter = createWhiteSpinnerAdapter(R.array.vrm_log_level_options);
        logLevelSpinner.setAdapter(logLevelAdapter);
    }

    private void bindActions() {
        toggleButton.setOnClickListener(v -> showSettings());
        findViewById(R.id.vrm_button_close_settings).setOnClickListener(v -> hideSettings());
        findViewById(R.id.vrm_settings_dismiss).setOnClickListener(v -> hideSettings());

        findViewById(R.id.vrm_button_pick_vrm).setOnClickListener(v ->
                openDocumentPicker(REQUEST_PICK_VRM, "*/*"));
        findViewById(R.id.vrm_button_pick_image).setOnClickListener(v ->
                openDocumentPicker(REQUEST_PICK_IMAGE, "image/*"));
        findViewById(R.id.vrm_button_use_solid).setOnClickListener(v -> {
            WallpaperPrefs.setBackgroundMode(this, 0);
            refreshFileLabels();
            updateImageAdjustmentVisibility();
            notifyUnity("OnBackgroundChanged", "");
            toast(R.string.vrm_saved_solid_background);
        });
        findViewById(R.id.vrm_button_reset_adjustment).setOnClickListener(v -> resetImageAdjustment());
        findViewById(R.id.vrm_button_reset_model_transform).setOnClickListener(v -> resetModelTransform());
        findViewById(R.id.vrm_button_save_profile_1).setOnClickListener(v -> saveProfile(1));
        findViewById(R.id.vrm_button_load_profile_1).setOnClickListener(v -> loadProfile(1));
        findViewById(R.id.vrm_button_save_profile_2).setOnClickListener(v -> saveProfile(2));
        findViewById(R.id.vrm_button_load_profile_2).setOnClickListener(v -> loadProfile(2));
        findViewById(R.id.vrm_button_save_profile_3).setOnClickListener(v -> saveProfile(3));
        findViewById(R.id.vrm_button_load_profile_3).setOnClickListener(v -> loadProfile(3));
        findViewById(R.id.vrm_button_reset_all_settings).setOnClickListener(v -> resetAllSettings());
        findViewById(R.id.vrm_button_set_wallpaper).setOnClickListener(v -> openWallpaperPicker());
        findViewById(R.id.vrm_button_reload_wallpaper).setOnClickListener(v -> reloadWallpaper());

        // Camera seekbars
        distanceSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToDistance(progress);
                distanceValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setCameraDistance(MainActivity.this, value);
                    notifyCameraChanged();
                }
            }
        });

        heightSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToHeight(progress);
                heightValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setCameraHeight(MainActivity.this, value);
                    notifyCameraChanged();
                }
            }
        });

        angleSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                int value = progressToAngle(progress);
                angleValueText.setText(getString(R.string.vrm_angle_value_format, value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setCameraAngle(MainActivity.this, value);
                    notifyCameraChanged();
                }
            }
        });

        renderScaleSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToRenderScale(progress);
                renderScaleValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setRenderScale(MainActivity.this, value);
                    notifyRuntimeSettingsChanged();
                }
            }
        });

        // Color seekbars
        SeekBar.OnSeekBarChangeListener colorListener = new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                updateColorPreview();
                if (!suppressUiCallbacks && fromUser) {
                    saveBackgroundColor();
                    notifyColorChanged();
                }
            }
        };
        redSeekBar.setOnSeekBarChangeListener(colorListener);
        greenSeekBar.setOnSeekBarChangeListener(colorListener);
        blueSeekBar.setOnSeekBarChangeListener(colorListener);

        hexColorEdit.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {
            }

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {
            }

            @Override
            public void afterTextChanged(Editable s) {
                if (suppressUiCallbacks) return;
                String hex = s.toString().trim();
                if (hex.length() == 7 && hex.startsWith("#")) {
                    try {
                        int color = Color.parseColor(hex);
                        suppressUiCallbacks = true;
                        redSeekBar.setProgress(Color.red(color));
                        greenSeekBar.setProgress(Color.green(color));
                        blueSeekBar.setProgress(Color.blue(color));
                        suppressUiCallbacks = false;
                        updateColorPreview();
                        saveBackgroundColor();
                        notifyColorChanged();
                    } catch (IllegalArgumentException ignored) {
                    }
                }
            }
        });

        // Image adjustment seekbars
        offsetXSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToOffset(progress);
                offsetXValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setBackgroundImageOffsetX(MainActivity.this, value);
                    notifyUnity("OnImageAdjustmentChanged", "");
                }
            }
        });

        offsetYSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToOffset(progress);
                offsetYValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setBackgroundImageOffsetY(MainActivity.this, value);
                    notifyUnity("OnImageAdjustmentChanged", "");
                }
            }
        });

        scaleSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToScale(progress);
                scaleValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setBackgroundImageScale(MainActivity.this, value);
                    notifyUnity("OnImageAdjustmentChanged", "");
                }
            }
        });

        modelOffsetXSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToOffset(progress);
                modelOffsetXValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setModelOffsetX(MainActivity.this, value);
                    notifyRuntimeSettingsChanged();
                }
            }
        });

        modelOffsetYSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToOffset(progress);
                modelOffsetYValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setModelOffsetY(MainActivity.this, value);
                    notifyRuntimeSettingsChanged();
                }
            }
        });

        modelOffsetZSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToOffset(progress);
                modelOffsetZValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setModelOffsetZ(MainActivity.this, value);
                    notifyRuntimeSettingsChanged();
                }
            }
        });

        modelScaleSeekBar.setOnSeekBarChangeListener(new SimpleSeekBarListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                float value = progressToScale(progress);
                modelScaleValueText.setText(formatFloat(value));
                if (!suppressUiCallbacks && fromUser) {
                    WallpaperPrefs.setModelScale(MainActivity.this, value);
                    notifyRuntimeSettingsChanged();
                }
            }
        });

        fitModeSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {
                if (!suppressUiCallbacks) {
                    WallpaperPrefs.setBackgroundImageFitMode(MainActivity.this, position);
                    notifyUnity("OnImageAdjustmentChanged", "");
                }
            }

            @Override
            public void onNothingSelected(AdapterView<?> parent) {
            }
        });

        targetFpsSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {
                if (!suppressUiCallbacks) {
                    WallpaperPrefs.setTargetFps(MainActivity.this, TARGET_FPS_VALUES[clamp(position, 0, TARGET_FPS_VALUES.length - 1)]);
                    notifyRuntimeSettingsChanged();
                }
            }

            @Override
            public void onNothingSelected(AdapterView<?> parent) {
            }
        });

        logLevelSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {
                if (!suppressUiCallbacks) {
                    WallpaperPrefs.setLogLevel(MainActivity.this, clamp(position, 0, 1));
                    notifyRuntimeSettingsChanged();
                }
            }

            @Override
            public void onNothingSelected(AdapterView<?> parent) {
            }
        });

        touchEnabledSwitch.setOnCheckedChangeListener((buttonView, isChecked) -> {
            if (!suppressUiCallbacks) {
                WallpaperPrefs.setTouchEnabled(MainActivity.this, isChecked);
                notifyRuntimeSettingsChanged();
            }
        });

        physBoneEnabledSwitch.setOnCheckedChangeListener((buttonView, isChecked) -> {
            if (!suppressUiCallbacks) {
                WallpaperPrefs.setPhysBoneEnabled(MainActivity.this, isChecked);
                notifyRuntimeSettingsChanged();
            }
        });

        fpsOverlaySwitch.setOnCheckedChangeListener((buttonView, isChecked) -> {
            if (!suppressUiCallbacks) {
                WallpaperPrefs.setFpsOverlayEnabled(MainActivity.this, isChecked);
                notifyRuntimeSettingsChanged();
            }
        });
    }

    // ===== Load / refresh =====

    private void loadCurrentSettings() {
        suppressUiCallbacks = true;

        distanceSeekBar.setProgress(distanceToProgress(WallpaperPrefs.getCameraDistance(this)));
        heightSeekBar.setProgress(heightToProgress(WallpaperPrefs.getCameraHeight(this)));
        angleSeekBar.setProgress(angleToProgress(Math.round(WallpaperPrefs.getCameraAngle(this))));
        renderScaleSeekBar.setProgress(renderScaleToProgress(WallpaperPrefs.getRenderScale(this)));

        redSeekBar.setProgress(colorToProgress(WallpaperPrefs.getBackgroundColorRed(this)));
        greenSeekBar.setProgress(colorToProgress(WallpaperPrefs.getBackgroundColorGreen(this)));
        blueSeekBar.setProgress(colorToProgress(WallpaperPrefs.getBackgroundColorBlue(this)));

        fitModeSpinner.setSelection(clamp(WallpaperPrefs.getBackgroundImageFitMode(this), 0, 3));
        offsetXSeekBar.setProgress(offsetToProgress(WallpaperPrefs.getBackgroundImageOffsetX(this)));
        offsetYSeekBar.setProgress(offsetToProgress(WallpaperPrefs.getBackgroundImageOffsetY(this)));
        scaleSeekBar.setProgress(scaleToProgress(WallpaperPrefs.getBackgroundImageScale(this)));
        modelOffsetXSeekBar.setProgress(offsetToProgress(WallpaperPrefs.getModelOffsetX(this)));
        modelOffsetYSeekBar.setProgress(offsetToProgress(WallpaperPrefs.getModelOffsetY(this)));
        modelOffsetZSeekBar.setProgress(offsetToProgress(WallpaperPrefs.getModelOffsetZ(this)));
        modelScaleSeekBar.setProgress(scaleToProgress(WallpaperPrefs.getModelScale(this)));
        targetFpsSpinner.setSelection(targetFpsToIndex(WallpaperPrefs.getTargetFps(this)));
        logLevelSpinner.setSelection(clamp(WallpaperPrefs.getLogLevel(this), 0, 1));
        touchEnabledSwitch.setChecked(WallpaperPrefs.getTouchEnabled(this));
        physBoneEnabledSwitch.setChecked(WallpaperPrefs.getPhysBoneEnabled(this));
        fpsOverlaySwitch.setChecked(WallpaperPrefs.getFpsOverlayEnabled(this));

        distanceValueText.setText(formatFloat(progressToDistance(distanceSeekBar.getProgress())));
        heightValueText.setText(formatFloat(progressToHeight(heightSeekBar.getProgress())));
        angleValueText.setText(getString(R.string.vrm_angle_value_format,
                progressToAngle(angleSeekBar.getProgress())));
        renderScaleValueText.setText(formatFloat(progressToRenderScale(renderScaleSeekBar.getProgress())));
        offsetXValueText.setText(formatFloat(progressToOffset(offsetXSeekBar.getProgress())));
        offsetYValueText.setText(formatFloat(progressToOffset(offsetYSeekBar.getProgress())));
        scaleValueText.setText(formatFloat(progressToScale(scaleSeekBar.getProgress())));
        modelOffsetXValueText.setText(formatFloat(progressToOffset(modelOffsetXSeekBar.getProgress())));
        modelOffsetYValueText.setText(formatFloat(progressToOffset(modelOffsetYSeekBar.getProgress())));
        modelOffsetZValueText.setText(formatFloat(progressToOffset(modelOffsetZSeekBar.getProgress())));
        modelScaleValueText.setText(formatFloat(progressToScale(modelScaleSeekBar.getProgress())));

        suppressUiCallbacks = false;

        updateColorPreview();
        refreshFileLabels();
        updateImageAdjustmentVisibility();
        updateModelInfoLabel(lastModelInfo);
        updateFpsInfoLabel(lastFpsInfo);
    }

    private void refreshFileLabels() {
        String vrmPath = WallpaperPrefs.getVrmPath(this);
        String vrmDisplayName = WallpaperPrefs.getVrmDisplayName(this);
        String label = !vrmDisplayName.isEmpty()
                ? vrmDisplayName
                : (vrmPath.isEmpty() ? "" : new File(vrmPath).getName());
        vrmPathText.setText(label.isEmpty()
                ? getString(R.string.vrm_no_vrm_selected)
                : label);

        int backgroundMode = WallpaperPrefs.getBackgroundMode(this);
        backgroundModeText.setText(backgroundMode == 0
                ? R.string.vrm_solid_background_mode
                : R.string.vrm_image_background_mode);
    }

    private void updateColorPreview() {
        int r = redSeekBar.getProgress();
        int g = greenSeekBar.getProgress();
        int b = blueSeekBar.getProgress();
        int color = Color.rgb(r, g, b);
        colorPreview.setBackgroundColor(color);

        String hex = String.format(Locale.US, "#%02X%02X%02X", r, g, b);
        if (!hexColorEdit.isFocused()) {
            suppressUiCallbacks = true;
            hexColorEdit.setText(hex);
            suppressUiCallbacks = false;
        }
    }

    private void updateImageAdjustmentVisibility() {
        imageAdjustmentPanel.setVisibility(
                WallpaperPrefs.getBackgroundMode(this) == 1 ? View.VISIBLE : View.GONE);
    }

    // ===== Unity notifications =====

    private void notifyUnity(String method, String message) {
        try {
            UnityPlayer.UnitySendMessage("VRMLoader", method, message);
        } catch (Exception e) {
            Log.w(TAG, "UnitySendMessage failed: " + method, e);
        }
    }

    private void notifyCameraChanged() {
        float d = progressToDistance(distanceSeekBar.getProgress());
        float h = progressToHeight(heightSeekBar.getProgress());
        float a = progressToAngle(angleSeekBar.getProgress());
        notifyUnity("OnCameraChanged",
                String.format(Locale.US, "%.4f,%.4f,%.4f", d, h, a));
    }

    private void notifyColorChanged() {
        float r = progressToColor(redSeekBar.getProgress());
        float g = progressToColor(greenSeekBar.getProgress());
        float b = progressToColor(blueSeekBar.getProgress());
        notifyUnity("OnBackgroundColorChanged",
                String.format(Locale.US, "%.4f,%.4f,%.4f", r, g, b));
    }

    private void notifyRuntimeSettingsChanged() {
        notifyUnity("OnRuntimeSettingsChanged", "");
    }

    // ===== Color =====

    private void saveBackgroundColor() {
        WallpaperPrefs.setBackgroundColorRed(this, progressToColor(redSeekBar.getProgress()));
        WallpaperPrefs.setBackgroundColorGreen(this, progressToColor(greenSeekBar.getProgress()));
        WallpaperPrefs.setBackgroundColorBlue(this, progressToColor(blueSeekBar.getProgress()));
    }

    // ===== Image adjustment =====

    private void resetImageAdjustment() {
        WallpaperPrefs.setBackgroundImageFitMode(this, 1);
        WallpaperPrefs.setBackgroundImageOffsetX(this, 0.0f);
        WallpaperPrefs.setBackgroundImageOffsetY(this, 0.0f);
        WallpaperPrefs.setBackgroundImageScale(this, 1.0f);
        loadCurrentSettings();
        notifyUnity("OnImageAdjustmentChanged", "");
        toast(R.string.vrm_saved_image_adjustment_reset);
    }

    private void resetModelTransform() {
        WallpaperPrefs.setModelOffsetX(this, 0.0f);
        WallpaperPrefs.setModelOffsetY(this, 0.0f);
        WallpaperPrefs.setModelOffsetZ(this, 0.0f);
        WallpaperPrefs.setModelScale(this, 1.0f);
        loadCurrentSettings();
        notifyRuntimeSettingsChanged();
        toast(R.string.vrm_saved_model_transform_reset);
    }

    private void saveProfile(int slot) {
        WallpaperPrefs.saveProfileSlot(this, slot);
        toastText(getString(R.string.vrm_saved_profile_format, slot));
    }

    private void loadProfile(int slot) {
        if (!WallpaperPrefs.loadProfileSlot(this, slot)) {
            toastText(getString(R.string.vrm_error_profile_not_found_format, slot));
            return;
        }

        loadCurrentSettings();
        applyAllSettingsToUnity();
        toastText(getString(R.string.vrm_loaded_profile_format, slot));
    }

    private void resetAllSettings() {
        WallpaperPrefs.resetRuntimeSettings(this);
        loadCurrentSettings();
        applyAllSettingsToUnity();
        toast(R.string.vrm_saved_all_settings_reset);
    }

    private void applyAllSettingsToUnity() {
        notifyCameraChanged();
        notifyColorChanged();
        notifyUnity("OnImageAdjustmentChanged", "");
        notifyUnity("OnBackgroundChanged", "");
        notifyRuntimeSettingsChanged();
        notifyUnity("ReloadVRM", "");
    }

    public static void updateModelInfoFromUnity(String info) {
        lastModelInfo = info == null ? "" : info;
        if (activeInstance != null) {
            activeInstance.runOnUiThread(() -> activeInstance.updateModelInfoLabel(lastModelInfo));
        }
    }

    public static void updateFpsInfoFromUnity(String info) {
        lastFpsInfo = info == null ? "" : info;
        if (activeInstance != null) {
            activeInstance.runOnUiThread(() -> activeInstance.updateFpsInfoLabel(lastFpsInfo));
        }
    }

    private void updateModelInfoLabel(String info) {
        if (modelInfoText == null) {
            return;
        }

        modelInfoText.setText(info == null || info.isEmpty()
                ? getString(R.string.vrm_model_info_empty)
                : info);
    }

    private void updateFpsInfoLabel(String info) {
        if (fpsInfoText == null) {
            return;
        }

        fpsInfoText.setText(info == null || info.isEmpty()
                ? getString(R.string.vrm_fps_info_empty)
                : info);
    }

    // ===== File picker =====

    private void openDocumentPicker(int requestCode, String mimeType) {
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType(mimeType);
        startActivityForResult(intent, requestCode);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (resultCode != RESULT_OK || data == null || data.getData() == null) {
            return;
        }

        Uri uri = data.getData();
        if (requestCode == REQUEST_PICK_VRM) {
            String displayName = getDisplayName(uri);
            String importedPath = importAvatarDocument(uri);
            if (importedPath != null) {
                WallpaperPrefs.setVrmPath(this, importedPath);
                WallpaperPrefs.setVrmDisplayName(this, displayName != null ? displayName : new File(importedPath).getName());
                refreshFileLabels();
                notifyUnity("ReloadVRM", "");
                toast(R.string.vrm_saved_vrm);
            }
            return;
        }

        if (requestCode == REQUEST_PICK_IMAGE) {
            String importedPath = importDocument(uri, "background", "png");
            if (importedPath != null) {
                WallpaperPrefs.setBackgroundImagePath(this, importedPath);
                WallpaperPrefs.setBackgroundMode(this, 1);
                refreshFileLabels();
                updateImageAdjustmentVisibility();
                notifyUnity("OnBackgroundChanged", "");
                toast(R.string.vrm_saved_image);
            }
        }
    }

    private String importAvatarDocument(Uri uri) {
        String displayName = getDisplayName(uri);
        String extension = resolveExtension(displayName, "");
        if (extension.isEmpty()) {
            extension = "assetbundle";
        }
        String targetBaseName = resolveBaseName(displayName, "avatar");
        return importDocument(uri, targetBaseName, extension);
    }

    private String importDocument(Uri uri, String targetBaseName, String fallbackExtension) {
        String displayName = getDisplayName(uri);
        String extension = resolveExtension(displayName, fallbackExtension);
        File targetDirectory = new File(getFilesDir(), "wallpaper_assets");
        if (!targetDirectory.exists() && !targetDirectory.mkdirs()) {
            toast(R.string.vrm_error_save_failed);
            return null;
        }

        String targetFileName = extension.isEmpty() ? targetBaseName : targetBaseName + "." + extension;
        File targetFile = new File(targetDirectory, targetFileName);
        try (InputStream inputStream = getContentResolver().openInputStream(uri);
             FileOutputStream outputStream = new FileOutputStream(targetFile, false)) {
            if (inputStream == null) {
                toast(R.string.vrm_error_open_failed);
                return null;
            }
            byte[] buffer = new byte[8192];
            int read;
            while ((read = inputStream.read(buffer)) != -1) {
                outputStream.write(buffer, 0, read);
            }
            outputStream.flush();
            return targetFile.getAbsolutePath();
        } catch (Exception exception) {
            toast(R.string.vrm_error_save_failed);
            return null;
        }
    }

    private String getDisplayName(Uri uri) {
        Cursor cursor = null;
        try {
            cursor = getContentResolver().query(uri, null, null, null, null);
            if (cursor != null && cursor.moveToFirst()) {
                int columnIndex = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                if (columnIndex >= 0) {
                    return cursor.getString(columnIndex);
                }
            }
        } finally {
            if (cursor != null) {
                cursor.close();
            }
        }
        return null;
    }

    private String resolveBaseName(String fileName, String fallbackBaseName) {
        String candidate = fallbackBaseName;
        if (fileName != null && !fileName.trim().isEmpty()) {
            int dotIndex = fileName.lastIndexOf('.');
            candidate = dotIndex > 0 ? fileName.substring(0, dotIndex) : fileName;
        }

        candidate = candidate.replaceAll("[\\\\/:*?\"<>|]", "_").trim();
        if (candidate.isEmpty()) {
            return fallbackBaseName;
        }

        return candidate;
    }

    private String resolveExtension(String fileName, String fallbackExtension) {
        if (fileName != null) {
            int dotIndex = fileName.lastIndexOf('.');
            if (dotIndex >= 0 && dotIndex < fileName.length() - 1) {
                return fileName.substring(dotIndex + 1).toLowerCase(Locale.US);
            }
        }
        return fallbackExtension == null ? "" : fallbackExtension;
    }

    // ===== Wallpaper actions =====

    private void openWallpaperPicker() {
        try {
            Intent intent = new Intent(WallpaperManager.ACTION_CHANGE_LIVE_WALLPAPER);
            intent.putExtra(WallpaperManager.EXTRA_LIVE_WALLPAPER_COMPONENT,
                    new ComponentName(this, WallpaperActivity.class));
            startActivity(intent);
        } catch (Exception primaryException) {
            try {
                startActivity(new Intent(WallpaperManager.ACTION_LIVE_WALLPAPER_CHOOSER));
            } catch (Exception secondaryException) {
                toast(R.string.vrm_error_wallpaper_picker);
            }
        }
    }

    private void reloadWallpaper() {
        WallpaperInfo wallpaperInfo = WallpaperManager.getInstance(this).getWallpaperInfo();
        boolean active = wallpaperInfo != null
                && WallpaperActivity.class.getName().equals(wallpaperInfo.getServiceName());
        if (!active) {
            toast(R.string.vrm_error_wallpaper_not_active);
            return;
        }

        Intent intent = new Intent("com.oreoreooooooo.VRM.RELOAD_VRM");
        intent.setPackage(getPackageName());
        sendBroadcast(intent);
        toast(R.string.vrm_wallpaper_reloaded);
    }

    // ===== Conversion helpers =====

    private int distanceToProgress(float value) {
        return Math.round((clamp(value, DISTANCE_MIN, DISTANCE_MAX) - DISTANCE_MIN) * 10.0f);
    }

    private int heightToProgress(float value) {
        return Math.round((clamp(value, HEIGHT_MIN, HEIGHT_MAX) - HEIGHT_MIN) * 20.0f);
    }

    private int angleToProgress(int value) {
        return clamp(value, ANGLE_MIN, ANGLE_MAX) - ANGLE_MIN;
    }

    private int colorToProgress(float value) {
        return clamp(Math.round(clamp(value, 0.0f, 1.0f) * 255.0f), 0, 255);
    }

    private int offsetToProgress(float value) {
        return Math.round((clamp(value, OFFSET_MIN, OFFSET_MAX) - OFFSET_MIN) * 100.0f);
    }

    private int scaleToProgress(float value) {
        return Math.round((clamp(value, SCALE_MIN, SCALE_MAX) - SCALE_MIN) * 100.0f);
    }

    private int renderScaleToProgress(float value) {
        return Math.round((clamp(value, RENDER_SCALE_MIN, RENDER_SCALE_MAX) - RENDER_SCALE_MIN) * 100.0f);
    }

    private float progressToDistance(int progress) {
        return DISTANCE_MIN + (progress / 10.0f);
    }

    private float progressToHeight(int progress) {
        return HEIGHT_MIN + (progress / 20.0f);
    }

    private int progressToAngle(int progress) {
        return progress + ANGLE_MIN;
    }

    private float progressToColor(int progress) {
        return progress / 255.0f;
    }

    private float progressToOffset(int progress) {
        return OFFSET_MIN + (progress / 100.0f);
    }

    private float progressToScale(int progress) {
        return SCALE_MIN + (progress / 100.0f);
    }

    private float progressToRenderScale(int progress) {
        return RENDER_SCALE_MIN + (progress / 100.0f);
    }

    private int targetFpsToIndex(int fps) {
        for (int i = 0; i < TARGET_FPS_VALUES.length; i++) {
            if (TARGET_FPS_VALUES[i] == fps) {
                return i;
            }
        }

        return 1;
    }

    private int clamp(int value, int min, int max) {
        return Math.max(min, Math.min(max, value));
    }

    private float clamp(float value, float min, float max) {
        return Math.max(min, Math.min(max, value));
    }

    private String formatFloat(float value) {
        return String.format(Locale.US, "%.2f", value);
    }

    private ArrayAdapter<CharSequence> createWhiteSpinnerAdapter(int arrayResourceId) {
        ArrayAdapter<CharSequence> adapter = ArrayAdapter.createFromResource(
                this, arrayResourceId, android.R.layout.simple_spinner_item);
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        return new ArrayAdapter<CharSequence>(this, android.R.layout.simple_spinner_item, adapterToArray(adapter)) {
            @Override
            public View getView(int position, View convertView, ViewGroup parent) {
                View view = super.getView(position, convertView, parent);
                styleSpinnerText(view, false);
                return view;
            }

            @Override
            public View getDropDownView(int position, View convertView, ViewGroup parent) {
                View view = super.getDropDownView(position, convertView, parent);
                styleSpinnerText(view, true);
                return view;
            }
        };
    }

    private CharSequence[] adapterToArray(ArrayAdapter<CharSequence> adapter) {
        CharSequence[] values = new CharSequence[adapter.getCount()];
        for (int i = 0; i < adapter.getCount(); i++) {
            values[i] = adapter.getItem(i);
        }
        return values;
    }

    private void styleSpinnerText(View view, boolean dropdown) {
        if (!(view instanceof TextView)) {
            return;
        }

        TextView textView = (TextView) view;
        textView.setTextColor(Color.WHITE);
        textView.setTextSize(14f);
        if (dropdown) {
            textView.setBackgroundColor(Color.parseColor("#DD1A1A1A"));
            textView.setPadding(24, 24, 24, 24);
        }
    }

    private void toast(int stringId) {
        Toast.makeText(this, stringId, Toast.LENGTH_SHORT).show();
    }

    private void toastText(String text) {
        Toast.makeText(this, text, Toast.LENGTH_SHORT).show();
    }

    private abstract static class SimpleSeekBarListener implements SeekBar.OnSeekBarChangeListener {
        @Override
        public void onStartTrackingTouch(SeekBar seekBar) {
        }

        @Override
        public void onStopTrackingTouch(SeekBar seekBar) {
        }
    }
}

package com.acls.staff.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val StaffColorScheme = lightColorScheme(
    primary = Green700,
    onPrimary = androidx.compose.ui.graphics.Color.White,
    primaryContainer = Green900,
    secondary = Teal600,
    error = Red700,
    background = Gray100,
    surface = androidx.compose.ui.graphics.Color.White
)

@Composable
fun StaffAppTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = StaffColorScheme,
        typography = Typography,
        content = content
    )
}

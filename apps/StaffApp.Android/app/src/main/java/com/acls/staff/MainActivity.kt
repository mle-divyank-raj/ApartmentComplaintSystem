package com.acls.staff

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.acls.staff.navigation.StaffNavGraph
import com.acls.staff.session.SessionManager
import com.acls.staff.ui.theme.StaffAppTheme
import dagger.hilt.android.AndroidEntryPoint
import javax.inject.Inject

@AndroidEntryPoint
class MainActivity : ComponentActivity() {

    @Inject
    lateinit var sessionManager: SessionManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            StaffAppTheme {
                StaffNavGraph(sessionManager = sessionManager)
            }
        }
    }
}

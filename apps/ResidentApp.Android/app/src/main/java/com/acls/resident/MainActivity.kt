package com.acls.resident

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.acls.resident.navigation.ResidentNavGraph
import com.acls.resident.session.SessionManager
import com.acls.resident.ui.theme.ResidentAppTheme
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
            ResidentAppTheme {
                ResidentNavGraph(sessionManager = sessionManager)
            }
        }
    }
}

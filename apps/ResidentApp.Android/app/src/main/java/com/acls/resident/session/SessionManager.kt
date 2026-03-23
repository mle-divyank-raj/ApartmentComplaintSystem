package com.acls.resident.session

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.intPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.runBlocking
import javax.inject.Inject
import javax.inject.Singleton

private val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "resident_session")

@Singleton
class SessionManager @Inject constructor(
    @ApplicationContext private val context: Context
) {
    companion object {
        private val KEY_ACCESS_TOKEN = stringPreferencesKey("access_token")
        private val KEY_USER_ID = intPreferencesKey("user_id")
        private val KEY_ROLE = stringPreferencesKey("role")
        private val KEY_PROPERTY_ID = intPreferencesKey("property_id")
    }

    // Called once on login; blocking write is acceptable at auth boundary.
    fun saveSession(accessToken: String, userId: Int, role: String, propertyId: Int) {
        runBlocking {
            context.dataStore.edit { prefs ->
                prefs[KEY_ACCESS_TOKEN] = accessToken
                prefs[KEY_USER_ID] = userId
                prefs[KEY_ROLE] = role
                prefs[KEY_PROPERTY_ID] = propertyId
            }
        }
    }

    fun clearSession() {
        runBlocking {
            context.dataStore.edit { it.clear() }
        }
    }

    fun getAccessToken(): String? = runBlocking {
        context.dataStore.data.map { it[KEY_ACCESS_TOKEN] }.firstOrNull()
    }

    fun isLoggedIn(): Boolean = getAccessToken() != null

    fun getUserId(): Int = runBlocking {
        context.dataStore.data.map { it[KEY_USER_ID] ?: 0 }.firstOrNull() ?: 0
    }

    fun getRole(): String = runBlocking {
        context.dataStore.data.map { it[KEY_ROLE] ?: "" }.firstOrNull() ?: ""
    }
}

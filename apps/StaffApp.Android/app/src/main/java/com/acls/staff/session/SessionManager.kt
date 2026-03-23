package com.acls.staff.session

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.intPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.runBlocking
import javax.inject.Inject
import javax.inject.Singleton

private val Context.dataStore by preferencesDataStore(name = "staff_session")

@Singleton
class SessionManager @Inject constructor(
    @ApplicationContext private val context: Context
) {
    companion object {
        private val KEY_ACCESS_TOKEN = stringPreferencesKey("access_token")
        private val KEY_USER_ID = intPreferencesKey("user_id")
        private val KEY_ROLE = stringPreferencesKey("role")
        private val KEY_STAFF_MEMBER_ID = intPreferencesKey("staff_member_id")
    }

    suspend fun saveSession(accessToken: String, userId: Int, role: String) {
        context.dataStore.edit { prefs ->
            prefs[KEY_ACCESS_TOKEN] = accessToken
            prefs[KEY_USER_ID] = userId
            prefs[KEY_ROLE] = role
        }
    }

    suspend fun saveStaffMemberId(staffMemberId: Int) {
        context.dataStore.edit { prefs ->
            prefs[KEY_STAFF_MEMBER_ID] = staffMemberId
        }
    }

    suspend fun clearSession() {
        context.dataStore.edit { it.clear() }
    }

    fun getAccessToken(): String? = runBlocking {
        context.dataStore.data.first()[KEY_ACCESS_TOKEN]
    }

    fun getStaffMemberId(): Int? = runBlocking {
        context.dataStore.data.first()[KEY_STAFF_MEMBER_ID]
    }

    fun isLoggedIn(): Flow<Boolean> = context.dataStore.data
        .map { prefs -> prefs[KEY_ACCESS_TOKEN] != null }
}
